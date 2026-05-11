using Api.Domain.Exceptions;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Infrastructure.Extensions;
using Api.Models.Requests;
using Api.Models.Responses;
using Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IListService
{
    public Task<Guid> CreateAsync(Guid userId, string name, Guid? categoryId, CancellationToken cancellationToken = default);
    public Task RenameAsync(Guid userId, Guid listId, string name, CancellationToken cancellationToken = default);
    public Task ToggleCompleteAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task ToggleArchiveAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task<SearchResultsResponseModel<ListSearchResult>> SearchAsync(Guid userId, ListSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    public Task<ListGetResult> GetAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task SetCategoryAsync(Guid userId, Guid listId, Guid? categoryId, CancellationToken cancellationToken = default);
    public Task<ListCountResult> GetCountsAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
}



/// <summary>
/// Service for managing lists. This includes creating, updating, archiving, and deleting lists.
/// It also includes methods for retrieving lists and their associated items.
/// </summary>
public class ListService : IListService
{
    private readonly IDbContextFactory<TodoDatabaseContext> _dbContextFactory;
    private readonly IEntityValidator<ListEntity> _validator;
    private readonly ILogger<ListService> _logger;

    public ListService(
        IDbContextFactory<TodoDatabaseContext> dbContextFactory,
        IEntityValidator<ListEntity> validator,
        ILogger<ListService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid userId, string name, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // if no category we don't care if it exists, otherwise check if category exists and can be used
        var categoryExists = !categoryId.HasValue? true : await _dbContext.Categories
            .WhereCurrentUserHasAccess(userId)
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
        
        if (!categoryExists)
        {
            throw new NotFoundException($"Category {categoryId} not found or access denied");
        }
        
        var newList = new ListEntity
        {
            OwnerId = userId,
            CategoryId = categoryId,
            Name = name,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        await _validator.ValidateAsync(newList);

        _dbContext.Lists.Add(newList);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created list {ListId} in category {CategoryId} for user {UserId}", newList.Id, categoryId, userId);

        return newList.Id;
    }

    public async Task RenameAsync(Guid userId, Guid listId, string name, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        list.Name = name;
        list.UpdatedOn = DateTime.UtcNow;

        await _validator.ValidateAsync(list);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Renamed list {ListId} to '{Name}' for user {UserId}", listId, name, userId);
    }

    public async Task ToggleCompleteAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        list.IsCompleted = !list.IsCompleted;
        list.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled completion status of list {ListId} for user {UserId}", listId, userId);
    }
    public async Task ToggleArchiveAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        list.Archived = !list.Archived;
        list.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled archive status of list {ListId} for user {UserId}", listId, userId);
    }

    public async Task SetCategoryAsync(Guid userId, Guid listId, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        var categoryExists = !categoryId.HasValue? true : await _dbContext.Categories
            .WhereCurrentUserHasAccess(userId)
            .AnyAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException();
        }

        list.CategoryId = categoryId;
        list.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set category of list {ListId} to {CategoryId} for user {UserId}", listId, categoryId, userId);
    }

    public async Task<SearchResultsResponseModel<ListSearchResult>> SearchAsync(Guid userId, ListSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = _dbContext.Lists
            .WhereSearchCriteria(searchCriteria, userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orderBy = searchCriteria.OrderBy != null 
            ? new[] { searchCriteria.OrderBy } 
            : new[] { new FieldOrderRequest { Field = "id", Ascending = true } };

        var results = await query
            .SortEntity(orderBy, ListEntity.SortMappings)
            .Skip(searchCriteria.Offset ?? 0)
            .Take(searchCriteria.PageSize ?? 20)
            .Select(l => new ListSearchResult
            {
                Id = l.Id,
                Name = l.Name,
                CategoryName = l.Category != null ? l.Category.Name : string.Empty,
                IsCompleted = l.IsCompleted,
                Archived = l.Archived,
                SoonestDueDate = _dbContext.ListItems
                    .Where(i => i.ParentId == l.Id && i.DueDate != null && !i.IsCompleted)
                    .OrderBy(i => i.DueDate)
                    .Select(i => i.DueDate)
                    .FirstOrDefault() ?? DateTime.MaxValue
            })
            .ToListAsync(cancellationToken);

        return new SearchResultsResponseModel<ListSearchResult>
        {
            TotalCount = totalCount,
            PageSize = searchCriteria.PageSize ?? 20,
            Offset = searchCriteria.Offset ?? 0,
            Items = results
        };
    }

    public async Task<ListGetResult> GetAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .Include(l => l.Category)
            .WhereCurrentUserHasAccess(userId)
            .Where(l => l.Id == listId)
            .FirstOrDefaultAsync(cancellationToken);

        if (list == null)
        {
            throw new NotFoundException();
        }

        return new ListGetResult
        {
            Id = list.Id,
            Name = list.Name,
            Category = list.Category?.Name ?? string.Empty,
            CategoryId = list.CategoryId,
            Archived = list.Archived,
            Completed = list.IsCompleted,
            TotalItems = list.Children?.Count ?? 0,
            CompletedItems = list.Children?.Count(c => c.IsCompleted) ?? 0
        };
    }

    public async Task<ListCountResult> GetCountsAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var counts = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .Where(l => l.Id == listId)
            .Select(l => new ListCountResult
            {
                TotalItems = _dbContext.ListItems.WhereCurrentUserHasAccess(userId).Count(i => i.ParentId == l.Id),
                CompletedItems = _dbContext.ListItems.WhereCurrentUserHasAccess(userId).Count(i => i.ParentId == l.Id && i.IsCompleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (counts == null)
        {
            throw new NotFoundException();
        }

        return counts;
    }
}

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
    public Task<Guid> CreateAsync(Guid userId, string name, Guid categoryId, CancellationToken cancellationToken = default);
    public Task RenameAsync(Guid userId, Guid listId, string name, CancellationToken cancellationToken = default);
    public Task CloseAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task OpenAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task DeleteAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    public Task<SearchResultsResponseModel<ListSearchResult>> SearchAsync(Guid userId, ListSearchCriteria searchCriteria, CancellationToken cancellationToken = default);
    public Task<ListGetResult> GetAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
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

    public async Task<Guid> CreateAsync(Guid userId, string name, Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // Verify category exists and user has access
        var categoryExists = await _dbContext.Categories
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

    public async Task CloseAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        list.Archived = true;
        list.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Archived list {ListId} for user {UserId}", listId, userId);
    }

    public async Task OpenAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        list.Archived = false;
        list.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unarchived list {ListId} for user {UserId}", listId, userId);
    }

    public async Task DeleteAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .FirstOrDefaultAsync(l => l.Id == listId, cancellationToken);
        
        if (list == null)
        {
            throw new NotFoundException();
        }

        _dbContext.Lists.Remove(list);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted list {ListId} for user {UserId}", listId, userId);
    }

    public async Task<SearchResultsResponseModel<ListSearchResult>> SearchAsync(Guid userId, ListSearchCriteria searchCriteria, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = _dbContext.Lists
            .WhereSearchCriteria(searchCriteria, userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orderBy = searchCriteria.OrderBy != null 
            ? new[] { searchCriteria.OrderBy } 
            : Array.Empty<FieldOrderRequest>();

        var results = await query
            .SortEntity(orderBy, ListEntity.SortMappings)
            .Skip(searchCriteria.Offset ?? 0)
            .Take(searchCriteria.PageSize ?? 20)
            .Select(l => new ListSearchResult
            {
                Id = l.Id,
                Name = l.Name,
                CategoryName = l.Category.Name,
                Archived = l.Archived,
                TotalItems = l.Children.Count,
                CompletedItems = l.Children.Count(c => c.IsCompleted)
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
            .Include(l => l.Children)
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
            Category = list.Category.Name,
            Archived = list.Archived,
            TotalItems = list.Children.Count,
            CompletedItems = list.Children.Count(c => c.IsCompleted),
            Items = list.Children.Select(c => new ListItemSearchResult
            {
                Id = c.Id,
                Name = c.Name,
                Category = list.Category.Name,
                Completed = c.IsCompleted,
                Archived = false
            }).ToList()
        };
    }
}

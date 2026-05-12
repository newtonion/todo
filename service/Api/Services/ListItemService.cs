using System;
using Api.Domain.Exceptions;
using Api.Infrastructure;
using Api.Infrastructure.Entities;
using Api.Infrastructure.Extensions;
using Api.Models.Requests;
using Api.Models.Responses;
using Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IListItemService
{
    public Task<Guid> CreateAsync(Guid userId, Guid listId, Guid? listItemParentId, string name, DateTime? dueDate, CancellationToken cancellationToken = default);

    public Task<SearchResultsResponseModel<ListItemSearchResult>> SearchAsync(Guid userId, ListItemSearchCriteria criteria, CancellationToken cancellationToken = default);

    public Task<ListItemGetResult> GetAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default);
    
    public Task<List<ListItemGetResult>> GetChildrenAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default);

    public Task RenameAsync(Guid userId, Guid listId, Guid itemId, string name, CancellationToken cancellationToken = default);

    public Task SetDueDateAsync(Guid userId, Guid listId, Guid itemId, DateTime? dueDate, CancellationToken cancellationToken = default);

    public Task DeleteAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default);

    public Task ToggleCompletionAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default);
}

public class ListItemService : IListItemService
{
    private readonly IDbContextFactory<TodoDatabaseContext> _dbContextFactory;
    private readonly IEntityValidator<ListItemEntity> _validator;
    private readonly ILogger<ListItemService> _logger;

    public ListItemService(
        IDbContextFactory<TodoDatabaseContext> dbContextFactory,
        IEntityValidator<ListItemEntity> validator,
        ILogger<ListItemService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _validator = validator;
        _logger = logger;
    }
    public async Task<Guid> CreateAsync(Guid userId, Guid listId, Guid? listItemParentId, string name, DateTime? dueDate, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        // Verify list exists and user has access
        var listExists = await _dbContext.Lists
            .WhereCurrentUserHasAccess(userId)
            .AnyAsync(l => l.Id == listId, cancellationToken);

        var listItemParentExists = listItemParentId == null ? true : await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .AnyAsync(li => li.Id == listItemParentId.Value, cancellationToken);
        
        if (!listExists)
        {
            throw new NotFoundException($"List {listId} not found or access denied");
        }
        if (!listItemParentExists)
        {
            throw new NotFoundException($"Parent list item {listItemParentId} not found or access denied");
        }
        
        // Verify parent doesn't already have 10 children
        if (listItemParentId.HasValue)
        {
            var childrenCount = await _dbContext.ListItems
                .Where(li => li.ParentListItemId == listItemParentId.Value)
                .CountAsync(cancellationToken);
            
            if (childrenCount >= 10)
            {
                throw new ValidationException("A list item cannot have more than 10 child items.");
            }
        }
        
        var newListItem = new ListItemEntity()
        {
            OwnerId = userId,
            Name = name,
            IsCompleted = false,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow,
            DueDate = dueDate,
            ParentId = listId,
            ParentListItemId = listItemParentId
        };

        await _validator.ValidateAsync(newListItem);
        
        _dbContext.ListItems.Add(newListItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created list item {ItemId} in list {ListId} for user {UserId}", newListItem.Id, listId, userId);

        return newListItem.Id;
    }

    public async Task<SearchResultsResponseModel<ListItemSearchResult>> SearchAsync(Guid userId, ListItemSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = _dbContext.ListItems
            .Include(li => li.Parent)
            .ThenInclude(p => p.Category)
            .WhereSearchCriteria(criteria, userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orderBy = criteria.OrderBy != null 
            ? new[] { criteria.OrderBy } 
            : new[] { new FieldOrderRequest { Field = "id", Ascending = true } };

        var results = await query
            .SortEntity(orderBy, ListItemEntity.SortMappings)
            .Skip(criteria.Offset ?? 0)
            .Take(criteria.PageSize ?? 20)
            .Select(li => new ListItemSearchResult
            {
                Id = li.Id,
                Name = li.Name,
                IsCompleted = li.IsCompleted,
                DueDate = li.DueDate,
            })
            .ToListAsync(cancellationToken);

        return new SearchResultsResponseModel<ListItemSearchResult>
        {
            TotalCount = totalCount,
            PageSize = criteria.PageSize ?? 20,
            Offset = criteria.Offset ?? 0,
            Items = results
        };
    }

    public async Task<ListItemGetResult> GetAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .Where(li => li.Id == itemId)
            .Select(li => new ListItemGetResult
            {
                Id = li.Id,
                Name = li.Name,
                IsCompleted = li.IsCompleted,
                DueDate = li.DueDate,
                SortIndex = li.SortIndex,
                ParentId = li.ParentId,
                ParentName = li.Parent.Name,
                CategoryName = li.Parent.Category != null ? li.Parent.Category.Name : string.Empty,
                CreatedOn = li.CreatedOn,
                UpdatedOn = li.UpdatedOn,
                HasChildren = li.Children.Any(),
                TotalChildren = li.Children.Count(),
                TotalChildrenCompleted = li.Children.Count(c => c.IsCompleted),
                SoonestChildDueDate = li.Children
                    .Where(c => c.DueDate != null)
                    .Max(c => c.DueDate)
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (item == null)
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        return item;
    }

    public async Task<List<ListItemGetResult>> GetChildrenAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .Where(li => li.Id == itemId)
            .SelectMany(li => li.Children)
            .Select(child =>
             new ListItemGetResult
                {
                    Id = child.Id,
                    Name = child.Name,
                    IsCompleted = child.IsCompleted,
                    DueDate = child.DueDate,
                    SortIndex = child.SortIndex,
                    ParentId = child.ParentId,
                    ParentName = child.Parent.Name,
                    CategoryName = child.Parent.Category != null ? child.Parent.Category.Name : string.Empty,
                    CreatedOn = child.CreatedOn,
                    UpdatedOn = child.UpdatedOn
                })
            .ToListAsync(cancellationToken);
        
        if (item == null || !item.Any())
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        return item;
    }

    public async Task RenameAsync(Guid userId, Guid listId, Guid itemId, string name, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .FirstOrDefaultAsync(li => li.Id == itemId, cancellationToken);
        
        if (item == null)
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        item.Name = name;
        item.UpdatedOn = DateTime.UtcNow;

        await _validator.ValidateAsync(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Renamed list item {ItemId} to '{Name}' for user {UserId}", itemId, name, userId);
    }

    public async Task SetDueDateAsync(Guid userId, Guid listId, Guid itemId, DateTime? dueDate, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .FirstOrDefaultAsync(li => li.Id == itemId, cancellationToken);
        
        if (item == null)
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        item.DueDate = dueDate;
        item.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set due date for list item {ItemId} to {DueDate} for user {UserId}", itemId, dueDate, userId);
    }

    public async Task DeleteAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .FirstOrDefaultAsync(li => li.Id == itemId, cancellationToken);
        
        if (item == null)
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        _dbContext.ListItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted list item {ItemId} for user {UserId}", itemId, userId);
    }

    public async Task ToggleCompletionAsync(Guid userId, Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var item = await _dbContext.ListItems
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(listId)
            .FirstOrDefaultAsync(li => li.Id == itemId, cancellationToken);
        
        if (item == null)
        {
            throw new NotFoundException($"List item {itemId} not found or access denied");
        }

        item.IsCompleted = !item.IsCompleted;
        item.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled completion for list item {ItemId} to {IsCompleted} for user {UserId}", itemId, item.IsCompleted, userId);
    }
}

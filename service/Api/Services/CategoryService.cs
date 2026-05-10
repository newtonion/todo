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

public interface ICategoryService
{
    public Task<Guid> CreateAsync(Guid userId, string name, CancellationToken cancellationToken = default);
    public Task UpdateAsync(Guid userId, Guid categoryId, string name, CancellationToken cancellationToken = default);
    public Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    public Task<SearchResultsResponseModel<CategorySearchResult>> SearchAsync(Guid userId, CategorySearchCriteria criteria, CancellationToken cancellationToken = default);
    public Task<CategorySearchResult> GetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for managing categories. Users can create their own categories, and global categories (Owner = null) are available to all users.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<TodoDatabaseContext> _dbContextFactory;
    private readonly IEntityValidator<CategoryEntity> _validator;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IDbContextFactory<TodoDatabaseContext> dbContextFactory,
        IEntityValidator<CategoryEntity> validator,
        ILogger<CategoryService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid userId, string name, CancellationToken cancellationToken = default)
    {
        var newCategory = new CategoryEntity
        {
            OwnerId = userId,
            Name = name,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        await _validator.ValidateAsync(newCategory);

        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        _dbContext.Categories.Add(newCategory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created category {CategoryId} '{Name}' for user {UserId}", newCategory.Id, name, userId);

        return newCategory.Id;
    }

    public async Task UpdateAsync(Guid userId, Guid categoryId, string name, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await _dbContext.Categories
            .WhereOwnedByUser(userId)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        
        if (category == null)
        {
            throw new NotFoundException();
        }

        category.Name = name;
        category.UpdatedOn = DateTime.UtcNow;

        await _validator.ValidateAsync(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated category {CategoryId} to '{Name}' for user {UserId}", categoryId, name, userId);
    }

    public async Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await _dbContext.Categories
            .WhereOwnedByUser(userId)
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        
        if (category == null)
        {
            throw new NotFoundException();
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted category {CategoryId} for user {UserId}", categoryId, userId);
    }

    public async Task<SearchResultsResponseModel<CategorySearchResult>> SearchAsync(Guid userId, CategorySearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = _dbContext.Categories
            .WhereCurrentUserHasAccess(userId)
            .WhereName(criteria.Text);

        var totalCount = await query.CountAsync(cancellationToken);

        var orderBy = criteria.OrderBy != null 
            ? new[] { criteria.OrderBy } 
            : new[] { new FieldOrderRequest { Field = "name", Ascending = true } };

        var results = await query
            .SortEntity(orderBy, CategoryEntity.SortMappings)
            .Skip(criteria.Offset ?? 0)
            .Take(criteria.PageSize ?? 50)
            .Select(c => new CategorySearchResult
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);

        return new SearchResultsResponseModel<CategorySearchResult>
        {
            TotalCount = totalCount,
            PageSize = criteria.PageSize ?? 50,
            Offset = criteria.Offset ?? 0,
            Items = results
        };
    }

    public async Task<CategorySearchResult> GetAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await _dbContext.Categories
            .WhereCurrentUserHasAccess(userId)
            .Where(c => c.Id == categoryId)
            .Select(c => new CategorySearchResult
            {
                Id = c.Id,
                Name = c.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
        {
            throw new NotFoundException();
        }

        return category;
    }
}

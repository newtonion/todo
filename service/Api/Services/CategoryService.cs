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
    public Task<SearchResultsResponseModel<CategorySearchResult>> SearchAsync(Guid userId, CategorySearchCriteria criteria, CancellationToken cancellationToken = default);

}

/// <summary>
/// Service for managing categories. We can search default categories. Users may eventually be able to create and own them.
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
            .Select(c => new CategorySearchResult
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync(cancellationToken);

        return new SearchResultsResponseModel<CategorySearchResult>
        {
            TotalCount = totalCount,
            Items = results
        };
    }
}

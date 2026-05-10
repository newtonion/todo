using System;
using Api.Infrastructure.Entities;

namespace Api.Infrastructure.Extensions;

public static class CategoryEntityExtensions
{
    public static IQueryable<CategoryEntity> WhereCurrentUserHasAccess(this IQueryable<CategoryEntity> query, Guid userId)
    {
        return query.Where(c => c.OwnerId == null || c.OwnerId == userId);
    }

    public static IQueryable<CategoryEntity> WhereOwnedByUser(this IQueryable<CategoryEntity> query, Guid userId)
    {
        return query.Where(c => c.OwnerId == userId);
    }

    public static IQueryable<CategoryEntity> WhereName(this IQueryable<CategoryEntity> query, string? nameCriteria)
    {
        if (string.IsNullOrEmpty(nameCriteria))
            return query;
        return query.Where(c => c.Name.Contains(nameCriteria));
    }
}

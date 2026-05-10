using System;
using Api.Infrastructure.Entities;
using Api.Models.Requests;

namespace Api.Infrastructure.Extensions;

public static class ListEntityExtensions
{
    public static IQueryable<ListEntity> WhereCurrentUserHasAccess(this IQueryable<ListEntity> query, Guid userId)
    {
        return query.Where(l => l.OwnerId == userId);
    }

    public static IQueryable<ListEntity> WhereSearchCriteria(this IQueryable<ListEntity> query, ListSearchCriteria searchCriteria, Guid userId)
    {
        return query
            .WhereCurrentUserHasAccess(userId)
            .WhereName(searchCriteria.Text)
            .WhereArchived(searchCriteria.IncludeArchived);
    }

    public static IQueryable<ListEntity> WhereName(this IQueryable<ListEntity> query, string? nameCriteria)
    {
        if (string.IsNullOrEmpty(nameCriteria))
            return query;
        return query.Where(l => l.Name.Contains(nameCriteria));
    }

    public static IQueryable<ListEntity> WhereArchived(this IQueryable<ListEntity> query, bool? includeArchived)
    {
        if (includeArchived == true)
            return query;
        return query.Where(l => !l.Archived);
    }
}

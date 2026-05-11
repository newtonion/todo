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
            .WhereCategoryText(searchCriteria.CategoryText)
            .WhereArchived(searchCriteria.IncludeArchived)
            .WhereCompleted(searchCriteria.IncludeCompleted)
            .WhereUpcomingOrOverdue(searchCriteria.OnlyUpcomingOrOverdue);
    }

    public static IQueryable<ListEntity> WhereName(this IQueryable<ListEntity> query, string? nameCriteria)
    {
        if (string.IsNullOrEmpty(nameCriteria))
            return query;
        return query.Where(l => l.Name.ToLower().Contains(nameCriteria.ToLower()));
    }

    public static IQueryable<ListEntity> WhereCategoryText(this IQueryable<ListEntity> query, string? categoryTextCriteria)
    {
        if (string.IsNullOrEmpty(categoryTextCriteria))
            return query;
        return query.Where(l => l.Category != null && l.Category.Name.ToLower().Contains(categoryTextCriteria.ToLower()));
    }

    public static IQueryable<ListEntity> WhereArchived(this IQueryable<ListEntity> query, bool? includeArchived)
    {
        if (includeArchived == true)
            return query;
        return query.Where(l => !l.Archived);
    }

    public static IQueryable<ListEntity> WhereCompleted(this IQueryable<ListEntity> query, bool? includeCompleted)
    {
        if (includeCompleted == true)
            return query;
        return query.Where(l => !l.IsCompleted);
    }

    public static IQueryable<ListEntity> WhereUpcomingOrOverdue(this IQueryable<ListEntity> query, bool? onlyUpcomingOrOverdue)
    {
        if (onlyUpcomingOrOverdue != true)
            return query;

        var upcomingWindow = DateTime.UtcNow.AddDays(2);
        return query.Where(l => l.Children.Any(x=> x.DueDate <= upcomingWindow && !x.IsCompleted));
    }
}

using System;
using Api.Infrastructure.Entities;
using Api.Models.Requests;

namespace Api.Infrastructure.Extensions;

public static class ListItemEntityExtensions
{
    public static IQueryable<ListItemEntity> WhereSearchCriteria(this IQueryable<ListItemEntity> query, ListItemSearchCriteria searchCriteria, Guid userId)
    {
        return query
            .WhereCurrentUserHasAccess(userId)
            .WhereParentList(searchCriteria.ListId)
            .WhereParentListItem(searchCriteria.ParentListItemId)
            .WhereName(searchCriteria.Text);
    }

    public static IQueryable<ListItemEntity> WhereCurrentUserHasAccess(this IQueryable<ListItemEntity> query, Guid userId)
    {
        return query.Where(li => li.OwnerId == userId);
    }

    public static IQueryable<ListItemEntity> WhereParentList(this IQueryable<ListItemEntity> query, Guid parentListId)
    {
        return query.Where(li => li.ParentId == parentListId);
    }

    public static IQueryable<ListItemEntity> WhereParentListItem(this IQueryable<ListItemEntity> query, Guid? parentListItemId)
    {
        if (!parentListItemId.HasValue)
            return query;
        return query.Where(li => li.ParentListItemId == parentListItemId.Value);
    }

    public static IQueryable<ListItemEntity> WhereName(this IQueryable<ListItemEntity> query, string? nameCriteria)
    {
        if (string.IsNullOrEmpty(nameCriteria))
            return query;
        return query.Where(li => li.Name.Contains(nameCriteria));
    }
}

using System;
using System.Linq.Expressions;
using Api.Models.Requests;

namespace Api.Infrastructure.Extensions;

public static class GenericEntityExtensions
{

    public static IQueryable<T> SortEntity<T>(
    this IQueryable<T> query,
    IEnumerable<FieldOrderRequest> sorts,
    Dictionary<string, Expression<Func<T, object>>> mappings)
{
    IOrderedQueryable<T>? orderedQuery = null;

    foreach (var sort in sorts)
    {
        if (!mappings.TryGetValue(sort.Field, out var expression))
        {
            continue;
        }

        if (orderedQuery == null)
        {
            orderedQuery = sort.Ascending
                ? query.OrderBy(expression)
                : query.OrderByDescending(expression);
        }
        else
        {
            orderedQuery = sort.Ascending
                ? orderedQuery!.ThenBy(expression)
                : orderedQuery!.ThenByDescending(expression);
        }
    }

    return orderedQuery ?? query;
}

}

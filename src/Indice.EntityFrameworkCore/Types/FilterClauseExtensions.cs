using Indice.Extensions;

namespace Indice.Types;

/// <summary>Entity framework Extensions related to <see cref="FilterClause"/>.</summary>
public static class FilterClauseQueryableExtensions
{
    /// <summary>Filters a sequence of values based on a predicate array of <see cref="FilterClause"/>.</summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source">An <see cref="IQueryable{TSource}"/> to filter.</param>
    /// <param name="predicateList">A list of <see cref="FilterClause"/> to use as predicates</param>
    /// <param name="dynamicMembers">The member in the source collection that is considered to be dynamic</param>
    /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input sequence that satisfy the conditions specified by predicate.</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, IEnumerable<FilterClause> predicateList, params string[] dynamicMembers) {
        var query = source;
        dynamicMembers ??= Microsoft.EntityFrameworkCore.Metadata.Builders.MappingExtensions.JsonColumns.GetValueOrDefault(typeof(TSource))?.ToArray() ?? Array.Empty<string>();

        if (predicateList is null || predicateList.Count() == 0) {
            return source;
        }
        foreach (var filter in predicateList) {
            query = query.Where(filter);
        }
        return query;
    }

    private static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source,
            FilterClause filter) {
        var where = DynamicExtensions.GetFullPredicateExpressionTree<TSource>(filter)!;
        return source.Where(where);
    }
}

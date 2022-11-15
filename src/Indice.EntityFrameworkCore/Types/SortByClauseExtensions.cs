using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Indice.Extensions;

namespace Indice.Types
{
    /// <summary>
    /// Extensions related to <see cref="SortByClause"/> and dynamic build of query expressions
    /// </summary>
    public static class SortByClauseExtensions
    {
        /// <summary>
        /// Order an <see cref="IQueryable{T}"/> by <paramref name="sorting"/>.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IQueryable{T}"/> contains.</typeparam>
        /// <param name="collection">The data source.</param>
        /// <param name="sorting">Contains member path and sort direction. Also contains the dataType</param>
        /// <param name="append">A flag indicating if the sort order will reset or be appended to the expression. Defaults to false</param>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> collection, SortByClause sorting, bool append = false) {
            var isPath = sorting.Path.IndexOf('.') > 0;
            var member = isPath ? sorting.Path[..sorting.Path.IndexOf('.')] : sorting.Path;
            var dynamicMembers = Microsoft.EntityFrameworkCore.Metadata.Builders.MappingExtensions.JsonColumns.GetValueOrDefault(typeof(T))?.ToHashSet();
            var isDynamic = dynamicMembers?.Contains(member, StringComparer.OrdinalIgnoreCase) == true;

            return isDynamic ? collection.ApplyJsonOrder(sorting, append) : collection.ApplyOrder(sorting.Path, sorting.Direction, append);
        }


        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a <see cref="ListOptions"/> sort expression.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <returns></returns>
        public static IOrderedQueryable<TSource> ApplyJsonOrder<TSource>(this IQueryable<TSource> source, SortByClause sorting, bool append) {
            var expression = DynamicExtensions.GetFullMemberExpressionTree<TSource>(sorting.Path, sorting.DataType);
            var returnType = expression.Body.Type;
            var methodPrefix = append && source is IOrderedQueryable<TSource> ? nameof(Queryable.ThenBy) : nameof(Queryable.OrderBy);
            var methodSuffix = sorting.Direction == SortByClause.DESC ? "Descending" : string.Empty;
            var methodName = methodPrefix + methodSuffix;
            var result = typeof(Queryable).GetMethods()
                                          .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2)
                                          .MakeGenericMethod(typeof(TSource), returnType)
                                          .Invoke(null, new object[] { source, expression });
            return (IOrderedQueryable<TSource>)result;
        }
    }
}

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
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> collection, SortByClause sorting) {
            var isPath = sorting.Path.IndexOf('.') > 0;
            var member = isPath ? sorting.Path[..sorting.Path.IndexOf('.')] : sorting.Path;
            var dynamicMembers = Microsoft.EntityFrameworkCore.Metadata.Builders.MappingExtensions.JsonColumns.GetValueOrDefault(typeof(T))?.ToHashSet();
            bool isDynamic = dynamicMembers?.Contains(member, StringComparer.OrdinalIgnoreCase) == true;
            
            return isDynamic ? collection.OrderByJson(sorting) : collection.OrderBy(sorting.Path, sorting.Direction);
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a <see cref="ListOptions"/> sort expression.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <returns></returns>
        public static IOrderedQueryable<TSource> OrderByJson<TSource>(this IQueryable<TSource> source, SortByClause sorting) {
            var expression = DynamicExtensions.GetFullMemberExpressionTree<TSource>(sorting.Path, sorting.DataType);
            var returnType = expression.Body.Type;
            var methodName = sorting.Direction == "DESC" ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
            var result = typeof(Queryable).GetMethods()
                                          .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2)
                                          .MakeGenericMethod(typeof(TSource), returnType)
                                          .Invoke(null, new object[] { source, expression });
            return (IOrderedQueryable<TSource>)result;
        }

        /// <summary>
        /// Sorts the elements of a sequence in descending order according to a <see cref="ListOptions"/> sort expression.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="query"></param>
        /// <param name="options"></param>
        /// <param name="dynamicMembers"></param>
        /// <returns></returns>
        public static IQueryable<TSource> ApplyJsonSort<TSource>(this IQueryable<TSource> query, ListOptions options, params string[] dynamicMembers) {
            foreach (var sort in options.GetSortings().Where(x => dynamicMembers.Any(jsonMember => x.Path.StartsWith(jsonMember, StringComparison.OrdinalIgnoreCase))).ToArray()) {
                switch (sort.Direction) {
                    case "DESC" when sort.DataType == JsonDataType.Integer:
                        query = query.OrderByDescending(DynamicExtensions.GetFullMemberExpressionTree<TSource, int>(sort.Path, sort.DataType));
                        break;
                    case "DESC" when sort.DataType == JsonDataType.Number:
                        query = query.OrderByDescending(DynamicExtensions.GetFullMemberExpressionTree<TSource, double>(sort.Path, sort.DataType));
                        break;
                    case "DESC" when sort.DataType == JsonDataType.DateTime:
                        query = query.OrderByDescending(DynamicExtensions.GetFullMemberExpressionTree<TSource, DateTime?>(sort.Path, sort.DataType));
                        break;
                    case "DESC":
                        query = query.OrderByDescending(DynamicExtensions.GetFullMemberExpressionTree<TSource, string>(sort.Path, sort.DataType));
                        break;
                    case "ASC" when sort.DataType == JsonDataType.Integer:
                        query = query.OrderBy(DynamicExtensions.GetFullMemberExpressionTree<TSource, int>(sort.Path, sort.DataType));
                        break;
                    case "ASC" when sort.DataType == JsonDataType.Number:
                        query = query.OrderBy(DynamicExtensions.GetFullMemberExpressionTree<TSource, double>(sort.Path, sort.DataType));
                        break;
                    case "ASC" when sort.DataType == JsonDataType.DateTime:
                        query = query.OrderBy(DynamicExtensions.GetFullMemberExpressionTree<TSource, DateTime?>(sort.Path, sort.DataType));
                        break;
                    default:
                        query = query.OrderBy(DynamicExtensions.GetFullMemberExpressionTree<TSource, string>(sort.Path, sort.DataType));
                        break;
                }
                options.RemoveSort(sort);
            }
            return query;
        }
    }
}

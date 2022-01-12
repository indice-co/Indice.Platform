using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Flattens a collection that contains descendants of the same type.
        /// </summary>
        /// <typeparam name="TSource">The type of object in the <see cref="IEnumerable{T}"/> collection.</typeparam>
        /// <param name="source">The collection that the mapping is applied.</param>
        /// <param name="selectorFunction">A predicate to apply in the collection.</param>
        /// <param name="getDescendantsFunction">A predicate to select the descendant objects from the collection.</param>
        public static IEnumerable<TSource> Map<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selectorFunction, Func<TSource, IEnumerable<TSource>> getDescendantsFunction) {
            if (source is null) {
                return Enumerable.Empty<TSource>();
            }
            var flattenedList = source.Where(selectorFunction);
            foreach (var element in source) {
                flattenedList = flattenedList.Concat(getDescendantsFunction(element).Map(selectorFunction, getDescendantsFunction));
            }
            return flattenedList;
        }
    }
}

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Extension methods on <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence <b>recursively</b>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of invoking the one-to-many transform function on each element of the input sequence.</returns>
        public static IEnumerable<TSource> SelectManyRecursive<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> selector) {
            if (source is null) {
                return Enumerable.Empty<TSource>();
            }
            return source.SelectMany(item => selector(item).SelectManyRecursive(selector).Prepend(item));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Types
{
    /// <summary>
    /// Async version of the Resultset extensions. These operate well with entity framework and <see cref="IQueryable"/>.
    /// </summary>
    public static class ResultSetExtensionsAsync
    {
        /// <summary>
        /// Asynchronous method that materializes an <see cref="IQueryable{T}"/> <paramref name="source"/> using the <seealso cref="ListOptions"/>
        /// for paging and sorting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="options">The options to use for sortig and paging</param>
        /// <returns>The results in a set that contains a page set of the total available records and the total count</returns>
        public static async Task<ResultSet<T>> ToResultSetAsync<T>(this IQueryable<T> source, ListOptions options) {
            options = options ?? new ListOptions();

            foreach (var sorting in options.GetSortings()) {
                source = source.OrderBy(sorting.Path, sorting.Direction);
            }

            return await source.ToResultSetAsync(options.Page, options.Size);
        }

        /// <summary>
        /// Asynchronous method that materializes an <see cref="IQueryable{T}"/> <paramref name="source"/> using 
        /// the <paramref name="page"/> number and <paramref name="size"/> for paging.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source collection</param>
        /// <param name="page">The page number</param>
        /// <param name="size">the page size</param>
        /// <returns>The results in a set that contains a page set of the total available records and the total count</returns>
        public static async Task<ResultSet<T>> ToResultSetAsync<T>(this IQueryable<T> source, int page, int size) {
            if (page <= 0) {
                throw new ArgumentOutOfRangeException(nameof(page), "Must be a positive integer");
            }

            if (size < 0) {
                throw new ArgumentOutOfRangeException(nameof(size), "Must be a positive integer");
            }

            var index = page - 1;

            if (size == 0) {
                return new ResultSet<T>(new T[0], await source.CountAsync());
            }

            return new ResultSet<T>(await source.Skip(index * size).Take(size).ToArrayAsync(), await source.CountAsync());
        }
    }
}

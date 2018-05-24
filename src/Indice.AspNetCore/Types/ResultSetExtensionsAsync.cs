using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Types
{
    public static class ResultSetExtensionsAsync
    {
        
        public static async Task<ResultSet<T>> ToResultSetAsync<T>(this IQueryable<T> source, ListOptions options) {
            options = options ?? new ListOptions();

            foreach (var sorting in options.GetSortings()) {
                source = source.OrderBy(sorting.Path, sorting.Direction);
            }

            return await source.ToResultSetAsync(options.Page, options.Size);
        }

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

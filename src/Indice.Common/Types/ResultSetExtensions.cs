using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.Types
{

    public static class ResultSetExtensions
    {
        public static ResultSet<T> ToResultSet<T>(this IList<T> collection) => new ResultSet<T>(collection, collection.Count);

        public static ResultSet<T> ToResultSet<T>(this IEnumerable<T> collection) => new ResultSet<T>(collection, collection.Count());

        public static ResultSet<T> ToResultSet<T>(this IEnumerable<T> collection, int count) => new ResultSet<T>(collection, count);

        public static ResultSet<T> ToResultSet<T>(this IQueryable<T> source, ListOptions options) {
            options = options ?? new ListOptions();

            foreach (var sorting in options.GetSortings()) {
                source = source.OrderBy(sorting.Path, sorting.Direction);
            }

            return source.ToResultSet(options.Page, options.Size);
        }

        public static ResultSet<T> ToResultSet<T>(this IQueryable<T> source, int page, int size) {
            if (page <= 0) {
                throw new ArgumentOutOfRangeException(nameof(page), "Must be a positive integer");
            }

            if (size < 0) {
                throw new ArgumentOutOfRangeException(nameof(size), "Must be a positive integer");
            }

            var index = page - 1;

            if (size == 0) {
                return new ResultSet<T>(new T[0], source.Count());
            }

            return new ResultSet<T>(source.Skip(index * size).Take(size), source.Count());
        }
        
    }
}

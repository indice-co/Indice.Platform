using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Mvc;

namespace Indice.AspNet.Types
{
    public class PaginatedList<T> : List<T>, IPaginatedList
    {
        public int Page { get; private set; }
        public int PageIndex => Page - 1;
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(IQueryable<T> source, int page, int pageSize) : this(source, page, pageSize, null, null) { }

        public PaginatedList(IQueryable<T> source, int page, int pageSize, string sortProp, string sortDir) {
            if (page <= 0) {
                throw new ArgumentOutOfRangeException(nameof(page), "Must be a positive integer");
            }

            if (pageSize < 0) {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Must be a positive integer");
            }

            Page = page;
            PageSize = pageSize;
            TotalCount = source.Count();
            var items = source;

            if (sortProp != null) {
                items = source.OrderBy(sortProp, sortDir);
            }

            items = items.Skip(PageIndex * PageSize).Take(PageSize);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            AddRange(items);
        }

        public PaginatedList(IEnumerable<T> source, int page, int pageSize, int totalCount) {
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            AddRange(source);
        }

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;
    }

    public static class PaginatedListExtensions
    {
        public static PaginatedList<T> ToPaginatedList<T>(this IQueryable<T> source, int page, int pageSize, string sortProp, string sortDir) => new PaginatedList<T>(source, page, pageSize, sortProp, sortDir);

        public static PaginatedList<T> ToPaginatedList<T>(this IQueryable<T> source, int page, int pageSize) => new PaginatedList<T>(source, page, pageSize);

        public static PaginatedList<T> ToPaginatedList<T>(this IEnumerable<T> source, int page, int pageSize) => new PaginatedList<T>(source, page, pageSize, source.Count());

        public static PaginatedList<T> ToPaginatedList<T>(this IEnumerable<T> source, int page, int pageSize, int count) => new PaginatedList<T>(source, page, pageSize, count);

        public static IPaginatedList AsPaginated(this IEnumerable source) => source as IPaginatedList;

        public static PaginatedList<T> AsPaginated<T>(this IEnumerable<T> source) => source as PaginatedList<T>;

        public static ExpandoObject ToExpando(this object anonymousObject) {
            IDictionary<string, object> anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(anonymousObject);
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var item in anonymousDictionary) {
                expando.Add(item);
            }

            return (ExpandoObject)expando;
        }
    }

    public interface IPaginatedList : IEnumerable
    {
        int Page { get; }
        int PageIndex { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    // http://stackoverflow.com/questions/41244/dynamic-linq-orderby
    public static class OrderByExtender
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> collection, string key, string direction) {
            if (direction.ToUpper() == "ASC") {
                return collection.OrderBy(key);
            } else {
                return collection.OrderByDescending(key);
            }
        }

        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property) => ApplyOrder(source, property, "OrderBy");

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property) => ApplyOrder(source, property, "OrderByDescending");

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property) => ApplyOrder(source, property, "ThenBy");

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property) => ApplyOrder(source, property, "ThenByDescending");

        static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName) {
            var props = property.Split('.');
            var type = typeof(T);
            var arg = Expression.Parameter(type, "x");
            Expression expr = arg;

            foreach (var prop in props) {
                // Use reflection (not ComponentModel) to mirror LINQ.
                var pi = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            var lambda = Expression.Lambda(delegateType, expr, arg);

            var result = typeof(Queryable).GetMethods()
                                          .Single(method => method.Name == methodName
                                            && method.IsGenericMethodDefinition
                                            && method.GetGenericArguments().Length == 2
                                            && method.GetParameters().Length == 2)
                                          .MakeGenericMethod(typeof(T), type)
                                          .Invoke(null, new object[] { source, lambda });

            return (IOrderedQueryable<T>)result;
        }
    }
}

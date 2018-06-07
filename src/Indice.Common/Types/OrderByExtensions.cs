using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Indice.Types
{
    /// <summary>
    /// By Marc Gravell of the AspNet Team 2008 http://stackoverflow.com/questions/41244/dynamic-linq-orderby
    /// </summary>
    public static class OrderByExtensions
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
#if NETSTANDARD14
                var pi = type.GetRuntimeProperty(prop);
#else
                var pi = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
#endif
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            var lambda = Expression.Lambda(delegateType, expr, arg);

#if NETSTANDARD14
            var result = typeof(IQueryable).GetRuntimeMethods()
                                           .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2)
                                           .MakeGenericMethod(typeof(T), type)
                                           .Invoke(null, new object[] { source, lambda });
#else
            var result = typeof(Queryable).GetMethods()
                                          .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2)
                                          .MakeGenericMethod(typeof(T), type)
                                          .Invoke(null, new object[] { source, lambda });
#endif
            return (IOrderedQueryable<T>)result;
        }
    }
}

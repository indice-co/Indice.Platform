using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Indice.Types
{
    /// <summary>
    /// By Marc Gravell of the ASP.NET Team 2008 http://stackoverflow.com/questions/41244/dynamic-linq-orderby
    /// </summary>
    public static class OrderByExtensions
    {
        /// <summary>
        /// Order an <see cref="IQueryable{T}"/> by string member path (<paramref name="property"/>) and <paramref name="direction"/> ASC, DESC.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IQueryable{T}"/> contains.</typeparam>
        /// <param name="collection">The data source.</param>
        /// <param name="property">The property name to use.</param>
        /// <param name="direction">ASC or DESC.</param>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> collection, string property, string direction) {
            if (direction.ToUpper() == "ASC") {
                return collection.OrderBy(property);
            } else {
                return collection.OrderByDescending(property);
            }
        }

        /// <summary>
        /// Order an <see cref="IQueryable{T}"/> by string member path (<paramref name="property"/>) in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IQueryable{T}"/> contains.</typeparam>
        /// <param name="source">The data source.</param>
        /// <param name="property">The property name to use.</param>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string property) => ApplyOrder(source, property, "OrderBy");

        /// <summary>
        /// Order an <see cref="IQueryable{T}"/> by string member path (<paramref name="property"/>) in descending order.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IQueryable{T}"/> contains.</typeparam>
        /// <param name="source">The data source.</param>
        /// <param name="property">The property name to use.</param>
        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string property) => ApplyOrder(source, property, "OrderByDescending");

        /// <summary>
        /// Order an <see cref="IOrderedQueryable{T}"/> by string member path (<paramref name="property"/>) in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IOrderedQueryable{T}"/> contains.</typeparam>
        /// <param name="source">The data source.</param>
        /// <param name="property">The property name to use.</param>
        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> source, string property) => ApplyOrder(source, property, "ThenBy");

        /// <summary>
        /// Order an <see cref="IOrderedQueryable{T}"/> by string member path (<paramref name="property"/>) in descending order.
        /// </summary>
        /// <typeparam name="T">The type of data that the <see cref="IOrderedQueryable{T}"/> contains.</typeparam>
        /// <param name="source">The data source.</param>
        /// <param name="property">The property name to use.</param>
        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> source, string property) => ApplyOrder(source, property, "ThenByDescending");

        private static IOrderedQueryable<T> ApplyOrder<T>(IQueryable<T> source, string property, string methodName) {
            var properties = property.Split('.');
            var type = typeof(T);
            var argument = Expression.Parameter(type, "x");
            Expression expression = argument;
            foreach (var prop in properties) {
                // Use reflection (not ComponentModel) to mirror LINQ.
                var propertyInfo = type.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                expression = Expression.Property(expression, propertyInfo);
                type = propertyInfo.PropertyType;
            }
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            var lambda = Expression.Lambda(delegateType, expression, argument);
            var result = typeof(Queryable).GetMethods()
                                          .Single(method => method.Name == methodName && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 2)
                                          .MakeGenericMethod(typeof(T), type)
                                          .Invoke(null, new object[] { source, lambda });
            return (IOrderedQueryable<T>)result;
        }
    }
}

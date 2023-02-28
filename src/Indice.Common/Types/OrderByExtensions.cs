using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Indice.Types;

/// <summary>
/// By Marc Gravell of the ASP.NET Team 2008 http://stackoverflow.com/questions/41244/dynamic-linq-orderby
/// </summary>
public static class OrderByExtensions
{
    /// <summary>
    /// can identify if the Queryable is indeed <see cref="IOrderedQueryable"/>
    /// </summary>
    /// <param name="source">The input queryable</param>
    /// <returns>True if it has already been sorted at least once</returns>
    public static bool IsOrdered(IQueryable source) => source.Expression.Type.IsGenericType && typeof(IOrderedQueryable<>).IsAssignableFrom(source.Expression.Type.GetGenericTypeDefinition()); 
    
    /// <summary>
    /// Order an <see cref="IQueryable{T}"/> by string member path (<paramref name="property"/>) and <paramref name="direction"/> ASC, DESC.
    /// </summary>
    /// <typeparam name="T">The type of data that the <see cref="IQueryable{T}"/> contains.</typeparam>
    /// <param name="collection">The data source.</param>
    /// <param name="property">The property name to use.</param>
    /// <param name="direction">ASC or DESC.</param>
    /// <param name="append">A flag indicating if the sort order will reset or be appended to the expression.</param>
    public static IOrderedQueryable<T> ApplyOrder<T>(this IQueryable<T> collection, string property, string direction, bool append) {
        var methodPrefix = append && IsOrdered(collection) ? nameof(Queryable.ThenBy) : nameof(Queryable.OrderBy);
        var methodSuffix = direction == SortByClause.DESC ? "Descending" : string.Empty;
        var methodName = methodPrefix + methodSuffix;
        return ApplyOrder(collection, property, methodName);
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

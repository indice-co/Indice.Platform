using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Indice.EntityFrameworkCore.Functions;

namespace Indice.Types
{
    /// <summary>
    /// Entity framework Extensions related to <see cref="FilterClause"/>.
    /// </summary>
    public static class FilterClauseQueryableExtensions
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate array of <see cref="FilterClause"/>.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">An <see cref="IQueryable{TSource}"/> to filter.</param>
        /// <param name="predicateList">A list of <see cref="FilterClause"/> to use as predicates</param>
        /// <param name="dynamicMembers">The member in the source collection that is considered to be dynamic</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input sequence that satisfy the conditions specified by predicate.</returns>
        public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, IEnumerable<FilterClause> predicateList, params string[] dynamicMembers) {
            var query = source;
            dynamicMembers ??= Microsoft.EntityFrameworkCore.Metadata.Builders.MappingExtensions.JsonColumns.GetValueOrDefault(typeof(TSource))?.ToArray() ?? Array.Empty<string>();

            if (predicateList is null || predicateList.Count() == 0) {
                return source;
            }
            foreach (var filter in predicateList) {
                query = query.ApplyJsonFilter(filter);
            }
            return query;
        }

        private static IQueryable<TSource> ApplyDynamicFilter<TSource>(this IQueryable<TSource> source, FilterClause filter) {
            return source;
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
                var path = "$." + sort.Path[sort.Path.IndexOf('.')..];
                var member = sort.Path[..sort.Path.IndexOf('.')];
                switch (sort.Direction) {
                    case "DESC" when sort.DataType == JsonDataType.Integer:
                    case "DESC" when sort.DataType == JsonDataType.Number:
                        query = query.OrderByDescending(x => JsonFunctions.CastToDouble(JsonFunctions.JsonValue(x, path)));
                        break;
                    case "DESC" when sort.DataType == JsonDataType.DateTime:
                        query = query.OrderByDescending(x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)));
                        break;
                    case "DESC":
                        query = query.OrderByDescending(x => JsonFunctions.JsonValue(x, path));
                        break;
                    case "ASC" when sort.DataType == JsonDataType.Integer:
                    case "ASC" when sort.DataType == JsonDataType.Number:
                        query = query.OrderBy(x => JsonFunctions.CastToDouble(JsonFunctions.JsonValue(x, path)));
                        break;
                    case "ASC" when sort.DataType == JsonDataType.DateTime:
                        query = query.OrderBy(x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)));
                        break;
                    default:
                        query = query.OrderBy(x => JsonFunctions.JsonValue(x, path));
                        break;
                }
                options.RemoveSort(sort);
            }
            return query;
        }

        /// <summary>
        /// Apply json filter
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="query"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static IQueryable<TSource> ApplyJsonFilter<TSource>(this IQueryable<TSource> query, FilterClause filter) {
            var path = "$" + filter.Member[filter.Member.IndexOf('.')..];
            var member = filter.Member[..filter.Member.IndexOf('.')];
            var selectorExpression = GetMemberExpression<TSource>(member);

            switch (filter.Operator) {
                // Equals
                case FilterOperator.Eq when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.Boolean && bool.TryParse(filter.Value, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToBoolean(JsonFunctions.JsonValue(x, path)) == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    var period = new Period { From = value.Date, To = value.Date.AddDays(1) };
                    query = query.Where<TSource, object>(selectorExpression, x => period.From <= JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) &&
                                             period.To > JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)));
                    break;
                case FilterOperator.Eq:
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.JsonValue(x, path) == filter.Value);
                    break;
                // Not equal
                case FilterOperator.Neq when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) != value || JsonFunctions.JsonValue(x, path) == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) != value || JsonFunctions.JsonValue(x, path) == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.Boolean && bool.TryParse(filter.Value, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToBoolean(JsonFunctions.JsonValue(x, path)) != value || JsonFunctions.JsonValue(x, path) == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    period = new Period { From = value.Date, To = value.Date.AddDays(1) };
                    query = query.Where<TSource, object>(selectorExpression, x => period.From > JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) &&
                                             period.To <= JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)));
                    break;
                case FilterOperator.Neq:
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.JsonValue(x, path) != filter.Value || JsonFunctions.JsonValue(x, path) == null);
                    break;
                // Greater than
                case FilterOperator.Gt when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) > value);
                    break;
                case FilterOperator.Gt when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) > value);
                    break;
                case FilterOperator.Gt when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) > value);
                    break;
                // Greater than or equal
                case FilterOperator.Gte when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) >= value);
                    break;
                case FilterOperator.Gte when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) >= value);
                    break;
                case FilterOperator.Gte when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) >= value);
                    break;
                // Less than
                case FilterOperator.Lt when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) < value);
                    break;
                case FilterOperator.Lt when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) < value);
                    break;
                case FilterOperator.Lt when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) < value);
                    break;
                // Less than or equal
                case FilterOperator.Lte when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToInt32(JsonFunctions.JsonValue(x, path)) <= value);
                    break;
                case FilterOperator.Lte when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => Convert.ToDouble(JsonFunctions.JsonValue(x, path)) <= value);
                    break;
                case FilterOperator.Lte when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)) <= value);
                    break;
                case FilterOperator.Contains:
                    query = query.Where<TSource, object>(selectorExpression, x => JsonFunctions.JsonValue(x, path).Contains(filter.Value));
                    break;
                case FilterOperator.In:
                    var values = filter.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    switch (filter.DataType) {
                        case JsonDataType.Integer:
                            var integers = values.Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : (int?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            query = query.Where<TSource, object>(selectorExpression, x => integers.Contains(Convert.ToInt32(JsonFunctions.JsonValue(x, path))));
                            break;
                        case JsonDataType.Number:
                            var doubles = values.Select(x => double.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : (double?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            query = query.Where<TSource, object>(selectorExpression, x => doubles.Contains(Convert.ToDouble(JsonFunctions.JsonValue(x, path))));
                            break;
                        case JsonDataType.DateTime:
                            var dates = values.Select(x => DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : (DateTime?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            query = query.Where<TSource, object>(selectorExpression, x => dates.Contains(JsonFunctions.CastToDate(JsonFunctions.JsonValue(x, path)).Value));
                            break;
                        case JsonDataType.String:
                        default:
                            query = query.Where<TSource, object>(selectorExpression, x => values.Contains(JsonFunctions.JsonValue(x, path)));
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(FilterOperator), filter.Operator, $"Cannot apply operator \"{filter.Operator}\" to a value of type \"{filter.DataType}\"");
            }
            return query;
        }

        private static IQueryable<TSource> Where<TSource, TMember>(this IQueryable<TSource> source,
                LambdaExpression propertySelector,
                Expression<Func<TMember, bool>> propertyPredicate) { 
            return source.Where(ParameterToMemberExpressionRebinder.Combine<TSource, TMember>(propertySelector, propertyPredicate));
        }

        private static LambdaExpression GetMemberExpression<T>(string property) {
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
            return Expression.Lambda(delegateType, expression, argument);
        }

        internal class ParameterToMemberExpressionRebinder : ExpressionVisitor
        {
            ParameterExpression _paramExpr;
            MemberExpression _memberExpr;

            ParameterToMemberExpressionRebinder(ParameterExpression paramExpr, MemberExpression memberExpr) {
                _paramExpr = paramExpr;
                _memberExpr = memberExpr;
            }

            public override Expression Visit(Expression p) {
                return base.Visit(p == _paramExpr ? _memberExpr : p);
            }

            public static Expression<Func<TSource, bool>> Combine<TSource, TMember>(
                LambdaExpression propertySelector,
                Expression<Func<TMember, bool>> propertyPredicate) {
                if (propertySelector.Body is not MemberExpression memberExpression) {
                    throw new ArgumentException("propertySelector");
                }
                var expr = Expression.Lambda<Func<TSource, bool>>(propertyPredicate.Body, propertySelector.Parameters);
                var rebinder = new ParameterToMemberExpressionRebinder(propertyPredicate.Parameters[0], memberExpression);
                return (Expression<Func<TSource, bool>>)rebinder.Visit(expr);
            }
        }
    }
}

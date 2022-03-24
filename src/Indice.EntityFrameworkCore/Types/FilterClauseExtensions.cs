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
                query = query.Where(filter);
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

        private static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source,
                FilterClause filter) {
            var getValue = GetFullPredicateExpressionTree<TSource>(filter);
            return source.Where(getValue);
        }

        private static Expression<Func<TSource, bool>> GetFullPredicateExpressionTree<TSource>(FilterClause filter) {
            var path = "$" + filter.Member[filter.Member.IndexOf('.')..];
            var member = filter.Member[..filter.Member.IndexOf('.')];
            var predicateExpression = GetPredicateExpression(filter);
            var castExpression = GetCastExpression(filter.DataType);
            var jsonValueExpression = GetJsonValueExpression(path);
            var memberExpression = GetMemberExpression<TSource>(member);
            if (castExpression is null)
                return (Expression<Func<TSource, bool>>)ParameterToMemberExpressionRebinder.Combine(ParameterToMemberExpressionRebinder.Combine(memberExpression, jsonValueExpression), predicateExpression);
            else 
                return (Expression<Func<TSource, bool>>)ParameterToMemberExpressionRebinder.Combine(ParameterToMemberExpressionRebinder.Combine(ParameterToMemberExpressionRebinder.Combine(memberExpression, jsonValueExpression), castExpression), predicateExpression);
        }

        /// <summary>
        /// x => x == value
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static LambdaExpression GetPredicateExpression(FilterClause filter) {
            LambdaExpression predicate;
            switch (filter.Operator) {
                // Equals
                case FilterOperator.Eq when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.Boolean && bool.TryParse(filter.Value, out var value):
                    predicate = (Expression<Func<bool, bool>>)(x => x == value);
                    break;
                case FilterOperator.Eq when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    var period = new Period { From = value.Date, To = value.Date.AddDays(1) };
                    predicate = (Expression<Func<DateTime?, bool>>)(x => period.From <= x && period.To > x);
                    break;
                case FilterOperator.Eq:
                    predicate = (Expression<Func<string, bool>>)(x => x == filter.Value);
                    break;
                // Not equal
                case FilterOperator.Neq when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x != value || x == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x != value || x == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.Boolean && bool.TryParse(filter.Value, out var value):
                    predicate = (Expression<Func<bool, bool>>)(x => x != value || x == null);
                    break;
                case FilterOperator.Neq when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    period = new Period { From = value.Date, To = value.Date.AddDays(1) };
                    predicate = (Expression<Func<DateTime?, bool>>)(x => period.From > x && period.To <= x);
                    break;
                case FilterOperator.Neq:
                    predicate = (Expression<Func<string, bool>>)(x => x != filter.Value || x == null);
                    break;
                // Greater than
                case FilterOperator.Gt when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x > value);
                    break;
                case FilterOperator.Gt when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x > value);
                    break;
                case FilterOperator.Gt when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    predicate = (Expression<Func<DateTime?, bool>>)(x => x > value);
                    break;
                // Greater than or equal
                case FilterOperator.Gte when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x >= value);
                    break;
                case FilterOperator.Gte when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x >= value);
                    break;
                case FilterOperator.Gte when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    predicate = (Expression<Func<DateTime?, bool>>)(x => x >= value);
                    break;
                // Less than
                case FilterOperator.Lt when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x < value);
                    break;
                case FilterOperator.Lt when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x < value);
                    break;
                case FilterOperator.Lt when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    predicate = (Expression<Func<DateTime?, bool>>)(x => x < value);
                    break;
                // Less than or equal
                case FilterOperator.Lte when filter.DataType == JsonDataType.Integer && int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<int, bool>>)(x => x <= value);
                    break;
                case FilterOperator.Lte when filter.DataType == JsonDataType.Number && double.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value):
                    predicate = (Expression<Func<double, bool>>)(x => x <= value);
                    break;
                case FilterOperator.Lte when filter.DataType == JsonDataType.DateTime && DateTime.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value):
                    predicate = (Expression<Func<DateTime?, bool>>)(x => x <= value);
                    break;
                // Contains
                case FilterOperator.Contains:
                    predicate = (Expression<Func<string, bool>>)(x => x.Contains(filter.Value));
                    break;
                case FilterOperator.In:
                    var values = filter.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    switch (filter.DataType) {
                        case JsonDataType.Integer:
                            var integers = values.Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : (int?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            predicate = (Expression<Func<int, bool>>)(x => integers.Contains(x));
                            break;
                        case JsonDataType.Number:
                            var doubles = values.Select(x => double.TryParse(x, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : (double?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            predicate = (Expression<Func<double, bool>>)(x => doubles.Contains(x));
                            break;
                        case JsonDataType.DateTime:
                            var dates = values.Select(x => DateTime.TryParse(x, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value) ? value : (DateTime?)null)
                                .Where(x => x.HasValue)
                                .Select(x => x.Value)
                                .ToList();
                            predicate = (Expression<Func<DateTime?, bool>>)(x => dates.Contains(x.Value));
                            break;
                        case JsonDataType.String:
                        default:
                            predicate = (Expression<Func<string, bool>>)(x => values.Contains(x));
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(FilterOperator), filter.Operator, $"Cannot apply operator \"{filter.Operator}\" to a value of type \"{filter.DataType}\"");
            }
            return predicate;
        }

        /// <summary>
        /// x => Convert.ToInt32(x)
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        private static LambdaExpression GetCastExpression(JsonDataType dataType) {
            var argument = Expression.Parameter(typeof(string), "x");
            MethodInfo castMethod;
            switch (dataType) {
                case JsonDataType.Integer:
                    castMethod = typeof(Convert).GetMethod(nameof(Convert.ToInt32), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    break;
                case JsonDataType.Number:
                    castMethod = typeof(Convert).GetMethod(nameof(Convert.ToDouble), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    break;
                case JsonDataType.Boolean:
                    castMethod = typeof(Convert).GetMethod(nameof(Convert.ToBoolean), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    break;
                case JsonDataType.DateTime:
                    castMethod = typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.CastToDate), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    break;
                case JsonDataType.String:
                default:
                    return null;
            }
            var body = Expression.Call(castMethod, argument);
            return Expression.Lambda(body, argument);
        }

        /// <summary>
        /// x => JsonFunctions.JsonValue(x, path)
        /// </summary>
        /// <param name="memberPath"></param>
        /// <returns></returns>
        private static LambdaExpression GetJsonValueExpression(string memberPath) {
            var path = "$" + memberPath[memberPath.IndexOf('.')..];
            var argument = Expression.Parameter(typeof(object), "x");
            MethodInfo jsonValueMethod = typeof(JsonFunctions).GetMethod(nameof(JsonFunctions.JsonValue), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            var body = Expression.Call(jsonValueMethod, argument, Expression.Constant(path));
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(string));
            return Expression.Lambda(delegateType, body, argument);
        }

        /// <summary>
        /// x => x.Member.Path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="memberPath"></param>
        /// <returns></returns>
        private static LambdaExpression GetMemberExpression<T>(string memberPath) {
            var properties = memberPath.Split('.');
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
            Expression _memberOrMethodCallExpr;

            ParameterToMemberExpressionRebinder(ParameterExpression paramExpr, Expression memberOrMethodCallExpr) {
                _paramExpr = paramExpr;
                _memberOrMethodCallExpr = memberOrMethodCallExpr;
            }

            public override Expression Visit(Expression p) {
                return base.Visit(p == _paramExpr ? _memberOrMethodCallExpr : p);
            }

            public static Expression<Func<TSource, bool>> CombineMemberSelectorWithPredicate<TSource, TMember>(
                LambdaExpression propertySelector,
                Expression<Func<TMember, bool>> propertyPredicate) {
                if (propertySelector.Body is not MemberExpression memberExpression) {
                    throw new ArgumentException("propertySelector");
                }
                var expr = Expression.Lambda<Func<TSource, bool>>(propertyPredicate.Body, propertySelector.Parameters);
                var rebinder = new ParameterToMemberExpressionRebinder(propertyPredicate.Parameters[0], memberExpression);
                return (Expression<Func<TSource, bool>>)rebinder.Visit(expr);
            }

            public static LambdaExpression Combine(
                LambdaExpression innerExpression,
                LambdaExpression outerExpression) {
                //Func<TSource, TOut>
                var delegateType = typeof(Func<,>).MakeGenericType(outerExpression.Body.Type, innerExpression.Parameters[0].Type);
                //var expr = Expression.Lambda(delegateType, lambdaExpression.Body, propertySelector.Parameters);
                var expr = Expression.Lambda(outerExpression.Body, innerExpression.Parameters);
                if (innerExpression.Body is MemberExpression memberExpression) {
                    var rebinder = new ParameterToMemberExpressionRebinder(outerExpression.Parameters[0], memberExpression);
                    return (LambdaExpression)rebinder.Visit(expr);
                } else if (innerExpression.Body is MethodCallExpression methodCallExpression) {
                    var rebinder = new ParameterToMemberExpressionRebinder(outerExpression.Parameters[0], methodCallExpression);
                    return (LambdaExpression)rebinder.Visit(expr);
                } else {
                    throw new ArgumentException("innerExpression");
                }
                
                
            }
        }
    }
}

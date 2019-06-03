using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Indice.Types
{
    /// <summary>
    /// Extension methods to manipulate <see cref="ListOptions"/>.
    /// </summary>
    public static class ListOptionsExtensions
    {
        /// <summary>
        /// Convert <see cref="ListOptions"/> to dictionary for use in a route value parameter scenario. This overload substitutes existing values with the ones 
        /// found inside the replacements object.
        /// </summary>
        /// <typeparam name="TReplacements"></typeparam>
        /// <param name="options"></param>
        /// <param name="replacements"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary<TReplacements>(this ListOptions options, TReplacements replacements) where TReplacements : class {
            if (null == replacements) {
                throw new ArgumentNullException(nameof(replacements));
            }

            var overrides = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase).Merge(replacements, typeof(TReplacements));
            var sortings = options.GetSortings();
            var sortKey = nameof(options.Sort).ToLower();

            if (overrides.ContainsKey(sortKey)) {
                var sort = overrides[sortKey].ToString();

                if (sortings.Any(s => s.Path.Equals(sort))) {
                    overrides[sortKey] = sortings.Select(s => s.Path.Equals(sort) ? s.NextState() : s).Where(s => s.HasValue).Select(s => s.Value).ToUriString();
                } else {
                    overrides[sortKey] = sortings.Union(new[] { SortByClause.Parse(sort) }).ToUriString();
                }
            }

            return options.ToDictionary().Merge(overrides, null);
        }

        /// <summary>
        /// Merge a <paramref name="dictionary"/> of route data with the values found inside the <paramref name="instance"/>.
        /// </summary>
        public static IDictionary<string, object> Merge(this IDictionary<string, object> dictionary, object instance, Type type = null, string prefix = null) {
            if (instance is IDictionary<string, object> other) {
                foreach (var keyValue in other) {
                    if (dictionary.ContainsKey(keyValue.Key)) {
                        dictionary[keyValue.Key] = keyValue.Value;
                    } else {
                        dictionary.Add(keyValue.Key, keyValue.Value);
                    }
                }

                return dictionary;
            } else if (instance is ListOptions options) {
                return dictionary.Merge(options.ToDictionary());
            }

            type = type ?? instance?.GetType();

            foreach (var property in type.GetRuntimeProperties()) {
                var value = property.GetValue(instance);

                if (value is ListOptions options) {
                    dictionary.Merge(options.ToDictionary());
                } else if (value != null) {
                    var textValue = GetStructValue(property.PropertyType, value);
                    var key = $"{prefix}{property.Name}";

                    if (!string.IsNullOrEmpty(textValue)) {
                        if (dictionary.ContainsKey(key)) {
                            dictionary[key] = textValue;
                        } else {
                            dictionary.Add(key, textValue);
                        }

                        continue;
                    }

                    var itemType = default(Type);

                    if (property.PropertyType.IsArray || (itemType = property.PropertyType.GetElementType()) != null) {
                        var array = ((IEnumerable)value).Cast<object>().Select(x => GetStructValue(itemType ?? x.GetType(), x)).ToArray();

                        if (dictionary.ContainsKey(key)) {
                            dictionary[key] = array;
                        } else {
                            dictionary.Add(key, array);
                        }
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Converts the <paramref name="source"/> <see cref="IEnumerable"/> of <seealso cref="SortByClause"/> to a value switable to use on the <seealso cref="ListOptions.Sort"/> property.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUriString(this IEnumerable<SortByClause> source) => string.Join(",", source.Select(s => s.ToString()));

        private static string GetStructValue(Type type, object value) {
            var textValue = string.Empty;

            if (type == typeof(DateTime?) && ((DateTime?)value).HasValue) {
                textValue = ((DateTime?)value).Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            } else if (type == typeof(DateTime) && ((DateTime)value) != default(DateTime)) {
                textValue = ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            } else if (
                type == typeof(string)
             || type == typeof(int)
             || type == typeof(int?)
             || type == typeof(decimal)
             || type == typeof(decimal?)
             || type == typeof(double)
             || type == typeof(double?)
             || type == typeof(Guid)
             || type == typeof(Guid?)
             ||
#if NETSTANDARD14
                type.GetTypeInfo().IsEnum || Nullable.GetUnderlyingType(type)?.GetTypeInfo().IsEnum == true) {
#else
                type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true) {
#endif
                textValue = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value);
            }

            return textValue;
        }

        public static IEnumerable<KeyValuePair<string, string>> AsRouteValues(this IDictionary<string, object> values) {
            return values.SelectMany(kv => {
                if (kv.Value == null) {
                    return null;
                }

                if (kv.Value.GetType().IsArray) {
                    if (kv.Key.ToLowerInvariant() == nameof(ListOptions.Sort).ToLowerInvariant()) {
                        return new[] {
                            new KeyValuePair<string, string>(kv.Key, string.Join(",", (IList)kv.Value))
                        };
                    }

                    return ((IList)kv.Value).Cast<object>().Select(x => new KeyValuePair<string, string>(kv.Key, x?.ToString()));
                }

                return new[] {
                    new KeyValuePair<string, string>(kv.Key, kv.Value.ToString())
                };
            });
        }

        /// <summary>
        /// Serialize the dictionary as a url forms encoded payload.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string ToFormUrlEncodedString(this IDictionary<string, object> values) {
            var parameters = values.AsRouteValues();
            return string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        }


        public static void AddSortRedirect<TSource, TDestination>(this ListOptions options, Expression<Func<TSource, object>> from, Expression<Func<TDestination, object>> to) {
            string GetExpressionMemberName(Expression expression, string parentPropertyName = null) {
                var isProperty = expression.NodeType == ExpressionType.MemberAccess && expression is MemberExpression;

                if (isProperty) {
                    var memberExpression = (MemberExpression)expression;
                    parentPropertyName = $"{memberExpression.Member.Name}{(parentPropertyName != null ? $".{parentPropertyName}" : string.Empty)}";
                    return GetExpressionMemberName(memberExpression.Expression, parentPropertyName);
                }

                return parentPropertyName;
            }

            var fromExpressionName = GetExpressionMemberName(from.Body);
            var toExpressionName = GetExpressionMemberName(to.Body);
            options.AddSortRedirect(fromExpressionName, toExpressionName);
        }
    }
}

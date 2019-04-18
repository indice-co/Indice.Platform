using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Humanizer;
using Microsoft.OpenApi.Any;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Extensions over Open Api
    /// </summary>
    public static class OpenApiExtensions
    {
        /// <summary>
        /// Converts an isntance to an openApi counterpart
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IOpenApiAny ToOpenApiAny<T>(this T instance) where T : class {
            return ToOpenApiAny(typeof(T), instance);
        }
        private static IOpenApiAny ToOpenApiAny(Type type, object instance) {
            var arrayResult = ToOpenApiArray(type, instance);
            if (arrayResult != null)
                return arrayResult;
            var result = new OpenApiObject();
            foreach (var property in type.GetRuntimeProperties()) {
                var value = property.GetValue(instance);

                if (value != null) {
                    var openValue = GetStructValue(property.PropertyType, value);
                    var key = property.Name.Camelize();

                    if (openValue != null) {
                        if (result.ContainsKey(key)) {
                            result[key] = openValue;
                        } else {
                            result.Add(key, openValue);
                        }
                        continue;
                    }

                    var array = default(OpenApiArray);
                    if ((array = ToOpenApiArray(property.PropertyType, value)) != null) {
                        if (result.ContainsKey(key)) {
                            result[key] = array;
                        } else {
                            result.Add(key, array);
                        }
                        continue;
                    }
                    var openObject = ToOpenApiAny(property.PropertyType, value);
                    if (result.ContainsKey(key)) {
                        result[key] = openObject;
                    } else {
                        result.Add(key, openObject);
                    }
                }
            }

            return result;
        }

        private static OpenApiArray ToOpenApiArray(Type type, object instance) {
            var itemType = GetAnyElementType(type);
            var array = default(OpenApiArray);
            if (itemType != null) {
                array = new OpenApiArray();
                array.AddRange(((IEnumerable)instance).Cast<object>().Select(x => GetStructValue(itemType ?? x.GetType(), x) ?? ToOpenApiAny(itemType ?? x.GetType(), x)));
            }
            return array;
        }

        private static IOpenApiPrimitive GetStructValue(Type type, object value) {
            var openValue = default(IOpenApiPrimitive);

            if (type == typeof(DateTime?) && ((DateTime?)value).HasValue) {
                openValue = new OpenApiDate(((DateTime?)value).Value.ToUniversalTime());
            } else if (type == typeof(DateTime) && ((DateTime)value) != default(DateTime)) {
                openValue = new OpenApiDate(((DateTime)value).ToUniversalTime());
            } else if (type == typeof(string)) {
                openValue = new OpenApiString((string)value);
            } else if (type == typeof(int) || type == typeof(int?)) {
                openValue = new OpenApiInteger((int)value);
            } else if (type == typeof(short) || type == typeof(short?)) {
                openValue = new OpenApiInteger((short)value);
            } else if (type == typeof(long) || type == typeof(long?)) {
                openValue = new OpenApiLong((long)value);
            } else if (type == typeof(float) || type == typeof(float?)) {
                openValue = new OpenApiFloat((float)value);
            } else if (type == typeof(decimal) || type == typeof(decimal?)) {
                openValue = new OpenApiDouble((double)(decimal)value);
            } else if (type == typeof(double) || type == typeof(double?)) {
                openValue = new OpenApiDouble((double)value);
            } else if (type == typeof(bool) || type == typeof(bool?)) {
                openValue = new OpenApiBoolean((bool)value);
            } else if (type == typeof(Guid) || type == typeof(Guid?)) {
                openValue = new OpenApiString($"{value}");
            } else if (type == typeof(byte) || type == typeof(byte?)) {
                openValue = new OpenApiByte((byte)value);
            } else if (
#if NETSTANDARD14
                type.GetTypeInfo().IsEnum || Nullable.GetUnderlyingType(type)?.GetTypeInfo().IsEnum == true) {
#else
                type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true) {
#endif
                openValue = new OpenApiString($"{value}");
            } else if (type.IsValueType && !type.IsPrimitive && !type.Namespace.StartsWith("System") && !type.IsEnum) {
                openValue = new OpenApiString($"{value}");
            }

            return openValue;
        }

        private static Type GetAnyElementType(Type type) {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType;
        }
    }
}

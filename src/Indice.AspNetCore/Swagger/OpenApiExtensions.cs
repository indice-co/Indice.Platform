using System;
using System.Collections;
using System.Reflection;
using Microsoft.OpenApi.Any;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Indice.AspNetCore.Swagger;

/// <summary>Extensions over Open Api</summary>
public static class OpenApiExtensions
{
    /// <summary>Converts an instance to an OpenApi counterpart.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    /// <returns></returns>
    public static IOpenApiAny ToOpenApiAny<T>(this T instance) where T : class => ToOpenApiAny(typeof(T), instance);

    private static IOpenApiAny ToOpenApiAny(Type type, object instance) {
        var arrayResult = ToOpenApiArray(type, instance);
        if (arrayResult != null) {
            return arrayResult;
        }
        var result = new OpenApiObject();
        foreach (var property in type.GetRuntimeProperties()) {
            var value = property.GetValue(instance);
            if (value != null) {
                var key = char.ToLower(property.Name[0]) + property.Name[1..];
                if (IsPrimitive(property.PropertyType)) {
                    var openValue = GetStructValue(property.PropertyType, value);
                    if (openValue != null) {
                        if (result.ContainsKey(key)) {
                            result[key] = openValue;
                        } else {
                            result.Add(key, openValue);
                        }
                        continue;
                    }
                } else {
                    var arrayOrDictionary = default(IOpenApiAny);
                    if ((arrayOrDictionary = ToOpenApiArray(property.PropertyType, value)) != null) {
                        if (result.ContainsKey(key)) {
                            result[key] = arrayOrDictionary;
                        } else {
                            result.Add(key, arrayOrDictionary);
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
        }
        return result;
    }

    private static IOpenApiAny ToOpenApiArray(Type type, object instance) {
        var itemType = GetAnyElementType(type);

        if (itemType != null) {
            var items = ((IEnumerable)instance).Cast<object>().Select(x => GetStructValue(itemType ?? x.GetType(), x) ?? ToOpenApiAny(itemType ?? x.GetType(), x));
            if (IsDictionary(type)) {
                var dictionary = new OpenApiObject();
                foreach (OpenApiObject item in items) {
                    dictionary.Add(((OpenApiString)item["key"]).Value, item["value"]);
                }
                return dictionary;
            }
            var array = new OpenApiArray();
            array.AddRange(items);
            return array;
        }
        return null;
    }

    private static bool IsDictionary(Type type) =>
        type.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>)).Any();

    private static bool IsPrimitive(Type type) =>
        type.IsValueType || type.IsPrimitive || type.IsEnum || type == typeof(string);

    private static IOpenApiPrimitive GetStructValue(Type type, object value) {
        return type switch {
            { } t when t == typeof(DateTime?) && ((DateTime?)value).HasValue => new OpenApiDate(((DateTime?)value).Value),
            { } t when t == typeof(DateTime) && ((DateTime)value) != default => new OpenApiDate((DateTime)value),
            { } t when t == typeof(DateTimeOffset) && ((DateTimeOffset)value) != default => new OpenApiDateTime((DateTimeOffset)value),
            { } t when t == typeof(DateTimeOffset?) && ((DateTimeOffset?)value).HasValue => new OpenApiDateTime(((DateTimeOffset?)value).Value),
            { } t when t == typeof(string) => new OpenApiString((string)value),
            { } t when t == typeof(int) || t == typeof(int?) => new OpenApiInteger((int)value),
            { } t when t == typeof(short) || t == typeof(short?) => new OpenApiInteger((short)value),
            { } t when t == typeof(long) || t == typeof(long?) => new OpenApiLong((long)value),
            { } t when t == typeof(float) || t == typeof(float?) => new OpenApiFloat((float)value),
            { } t when t == typeof(decimal) || t == typeof(decimal?) => new OpenApiDouble((double)(decimal)value),
            { } t when t == typeof(double) || t == typeof(double?) => new OpenApiDouble((double)value),
            { } t when t == typeof(bool) || t == typeof(bool?) => new OpenApiBoolean((bool)value),
            { } t when t == typeof(Guid) || t == typeof(Guid?) => new OpenApiString($"{value}"),
            { } t when t == typeof(byte) || t == typeof(byte?) => new OpenApiByte((byte)value),
            { } t when t.IsEnum || Nullable.GetUnderlyingType(t)?.IsEnum == true => new OpenApiString($"{value}"),
            { } t when t.IsValueType && !t.IsPrimitive && !t.Namespace.StartsWith("System") && !t.IsEnum => new OpenApiString($"{value}"),
            _ => default
        };
    }

    private static Type GetAnyElementType(Type type) {
        // Type is Array. Short-circuit if you expect lots of arrays.
        if (type.IsArray) {
            return type.GetElementType();
        }
        // Type is IEnumerable<T>.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            return type.GetGenericArguments()[0];
        }
        // Type implements/extends IEnumerable<T>.
        var enumType = type.GetInterfaces()
                           .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                           .Select(x => x.GenericTypeArguments[0]).FirstOrDefault();
        return enumType;
    }
}

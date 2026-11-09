#if NET10_0_OR_GREATER

namespace Microsoft.OpenApi.Models;

/// <summary>Extensions over Open Api to generate example models </summary>
public static class OpenApiModelExtensions
{
    

    internal static bool IsDictionary(this Type type) =>
        type.GetInterfaces().Concat([type]).Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>)).Any();

    internal static bool IsPrimitive(this Type type) =>
        type.IsValueType || type.IsPrimitive || type.IsEnum || type == typeof(string);

    internal static bool TryGetAnyElementType(this Type type, out Type? elementType) {
        elementType = type.GetAnyElementType();
        return elementType != null;
    }

    internal static Type? GetAnyElementType(this Type type) {
        // Type is Array. Short-circuit if you expect lots of arrays.
        if (type.IsArray || type.HasElementType) {
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
#endif

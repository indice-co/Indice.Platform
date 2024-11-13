#if NET5_0
using System.Buffers;
#endif
using System.Dynamic;
using System.Reflection;
using System.Text.Json;

namespace Indice.Extensions;

/// <summary>Extension methods on object.</summary>
public static class ObjectExtensions
{
    /// <summary>Converts an object to an <see cref="ExpandoObject"/>.</summary>
    /// <param name="value">The object to convert.</param>
    public static ExpandoObject? ToExpandoObject(this object? value) {
        if (value is null) {
            return default;
        }
        if (value is ExpandoObject expando) {
            return expando;
        }
        if (value is JsonElement json) {
            return json.Deserialize<ExpandoObject>()!;
        }

        var dataType = value.GetType();
        IDictionary<string, object?> obj = new ExpandoObject();
        foreach (var property in dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetIndexParameters().Length == 0)) {
            obj.Add(property.Name, property.GetValue(value, null));
        }
        return (ExpandoObject)obj;
    }
}

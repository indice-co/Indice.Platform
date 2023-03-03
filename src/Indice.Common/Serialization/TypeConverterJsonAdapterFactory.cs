using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>
/// A factory used to create various <see cref="TypeConverterJsonAdapter{T}"/> instances.
/// </summary>
public class TypeConverterJsonAdapterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) {
        var hasConverter = typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
        if (!hasConverter) {
            return false;
        }
        return true;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var converterType = typeof(TypeConverterJsonAdapter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>
/// ValueTuples are currently not supported by System.Text.Json since they require field support and System.Text.Json only supports public properties currently.
/// This factory takes care of this support for tupples up to 5 Items.
/// </summary>
public class ValueTupleJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) {
        var iTuple = typeToConvert.GetInterface("System.Runtime.CompilerServices.ITuple");
        return iTuple != null;
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var genericArguments = typeToConvert.GetGenericArguments();
        var converterType = genericArguments.Length switch {
            1 => typeof(ValueTupleJsonConverter<>).MakeGenericType(genericArguments),
            2 => typeof(ValueTupleJsonConverter<,>).MakeGenericType(genericArguments),
            3 => typeof(ValueTupleJsonConverter<,,>).MakeGenericType(genericArguments),
            4 => typeof(ValueTupleJsonConverter<,,,>).MakeGenericType(genericArguments),
            5 => typeof(ValueTupleJsonConverter<,,,,>).MakeGenericType(genericArguments),
            // And add other cases as needed
            _ => throw new NotSupportedException(),
        };
        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

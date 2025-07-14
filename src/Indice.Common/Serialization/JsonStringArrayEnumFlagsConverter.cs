using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Indice.Extensions;

namespace Indice.Serialization;

/// <summary>A factory that generates instances of <see cref="JsonStringArrayEnumFlagsConverter{TEnum}"/>.</summary>
public class JsonStringArrayEnumFlagsConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsFlagsEnum();

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        var converterType = typeof(JsonStringArrayEnumFlagsConverter<>).MakeGenericType(typeToConvert);
        var converter = Activator.CreateInstance(converterType)!;
        return (JsonConverter)converter;
    }
}

/// <summary>A custom JSON converter which transforms <see cref="Enum"/> flags to string array.</summary>
/// <typeparam name="TEnum">The type of the enum.</typeparam>
internal class JsonStringArrayEnumFlagsConverter<TEnum> : JsonConverter<TEnum>
{
    /// <inheritdoc />
    /// <remarks>https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-6-0#error-handling</remarks>
    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        if (reader.TokenType == JsonTokenType.String && Enum.TryParse(underlyingType ?? typeToConvert, reader.GetString()!, out var enumValue)) {
            return (TEnum)enumValue;
        } else if (reader.TokenType != JsonTokenType.StartArray) {
            throw new JsonException();
        }
        var enumValues = new List<string>();
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray) {
                return (TEnum)Enum.Parse(underlyingType ?? typeToConvert, string.Join(", ", enumValues), ignoreCase: true);
            } else if (reader.TokenType == JsonTokenType.String) {
                enumValues.Add(reader.GetString()!);
            } else {
                throw new JsonException();
            }
        }
        // In case of truncated json.
        throw new JsonException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options) {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        foreach (var enumValue in value.ToString()!.Split(',')) {
            writer.WriteStringValue(enumValue.Trim());
        }
        writer.WriteEndArray();
    }

}

/// <summary>
/// This resolver is used to ensure that the JSON type information for enum flags is correctly handled
/// </summary>
public class JsonStringArrayEnumFlagsTypeInfoResolver : IJsonTypeInfoResolver
{
    /// <inheritdoc />
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) {
        if (!type.IsFlagsEnum()) {
            return null;
        }
        
        var enumType = Nullable.GetUnderlyingType(type);
        var isNullable = enumType != null;
        if (!isNullable) {
            enumType = type;
        }
        var listType = typeof(List<>).MakeGenericType(enumType!);
        var typeInfoList = options.GetTypeInfo(listType);
        var typeInfo = options.GetTypeInfo(enumType!);
        return typeInfo;
    }
}

/// <summary>
/// Extensions for <see cref="JsonSerializerOptions"/> to add support for serializing and deserializing enum flags as string arrays.
/// </summary>
public static class JsonStringArrayEnumFlagsExtensions
{
    /// <summary>
    /// Adds support for serializing and deserializing enum flags as string arrays in JSON.
    /// </summary>
    /// <param name="options">The json serializer options to configure</param>
    /// <returns>The options for further configuration</returns>
    public static JsonSerializerOptions AddFlagsArraySupport(this JsonSerializerOptions options) {
        if (!options.Converters.OfType<JsonStringArrayEnumFlagsConverterFactory>().Any()) {
            // register the factory converter only once
            options.Converters.Insert(0, new JsonStringArrayEnumFlagsConverterFactory());
            //options.TypeInfoResolverChain.Insert(0, new JsonStringArrayEnumFlagsTypeInfoResolver());
            
            //options.TypeInfoResolver = options.TypeInfoResolver?.WithAddedModifier(jsonTypeInfo => {
                //foreach (var jsonPropertyInfo in jsonTypeInfo.Properties) {
                //    if (!jsonPropertyInfo.PropertyType.IsFlagsEnum()) {
                //        continue;
                //    }
                //    jsonPropertyInfo.CustomConverter = new JsonStringArrayEnumFlagsConverterFactory().CreateConverter(jsonPropertyInfo.PropertyType, options);
                //}
            //});
        }
        return options;
    }
}
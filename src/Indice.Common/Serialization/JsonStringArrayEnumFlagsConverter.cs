using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// A factory that generates instances of <see cref="JsonStringArrayEnumFlagsConverter{TEnum}"/>.
    /// </summary>
    public class JsonStringArrayEnumFlagsConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum && typeToConvert.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any();

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            var converterType = typeof(JsonStringArrayEnumFlagsConverter<>).MakeGenericType(typeToConvert);
            var converter = Activator.CreateInstance(converterType);
            return (JsonConverter)converter;
        }
    }

    /// <summary>
    /// A custom JSON converter which transforms <see cref="Enum"/> flags to string array.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enum.</typeparam>
    public class JsonStringArrayEnumFlagsConverter<TEnum> : JsonConverter<TEnum> where TEnum : Enum
    {
        /// <inheritdoc />
        /// <remarks>https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-6-0#error-handling</remarks>
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return default;
            }
            if (reader.TokenType != JsonTokenType.StartArray) {
                throw new JsonException();
            }
            var enumValues = new List<string>();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndArray) {
                    return (TEnum)Enum.Parse(typeToConvert, string.Join(", ", enumValues));
                } else if (reader.TokenType == JsonTokenType.String) {
                    enumValues.Add(reader.GetString());
                } else {
                    throw new JsonException();
                }
            }
            // In case of truncated json.
            throw new JsonException();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) {
            writer.WriteStartArray();
            foreach (var enumValue in value.ToString().Split(',')) {
                writer.WriteStringValue(enumValue.Trim());
            }
            writer.WriteEndArray();
        }
    }
}

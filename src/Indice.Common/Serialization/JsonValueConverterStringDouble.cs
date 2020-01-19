using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's double representation.
    /// </summary>
    public class JsonConverterStringDouble : JsonConverter<double>
    {
        /// <inheritdoc/>
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return default;
            }
            if (reader.TokenType == JsonTokenType.String) {
                if (double.TryParse(reader.GetString(), out var doubleValue)) {
                    return doubleValue;
                }
            }
            if (reader.TokenType == JsonTokenType.Number) {
                if (reader.TryGetDouble(out var doubleValue)) {
                    return doubleValue;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
}

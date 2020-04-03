using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's decimal representation.
    /// </summary>
    public class JsonStringDecimalConverter : JsonConverter<decimal>
    {
        /// <inheritdoc/>
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            // Deserialize JSON null literal to non-nullable type.
            if (reader.TokenType == JsonTokenType.Null) {
                return default;
            }
            if (reader.TokenType == JsonTokenType.String) {
                if (decimal.TryParse(reader.GetString(), NumberStyles.Currency, CultureInfo.InvariantCulture, out var decimalValue)) {
                    return decimalValue;
                }
            }
            if (reader.TokenType == JsonTokenType.Number) {
                if (reader.TryGetDecimal(out var decimalValue)) {
                    return decimalValue;
                }
            }
            return default;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
}

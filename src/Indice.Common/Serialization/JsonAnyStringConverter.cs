using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// A custom <see cref="JsonConverter"/> that tries to convert any JSON value a suitable string representation if possible. If not it takes the json and passes it to the value as a json string.
    /// </summary>
    public class JsonAnyStringConverter : JsonConverter<string>
    {
        /// <inheritdoc/>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return default;
            }
            if (reader.TokenType == JsonTokenType.String) {
                return reader.GetString();
            }
            if (reader.TokenType == JsonTokenType.Number) {
                if (reader.TryGetInt64(out var l)) {
                    return l.ToString(CultureInfo.InvariantCulture);
                }
                return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            }
            if (reader.TokenType == JsonTokenType.True) {
                return bool.TrueString.ToLower();
            }
            if (reader.TokenType == JsonTokenType.False) {
                return bool.FalseString.ToLower();
            }
            using (var document = JsonDocument.ParseValue(ref reader)) {
                return JsonSerializer.Serialize(document.RootElement, options);
            }
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }
}

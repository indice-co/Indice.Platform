using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a float JSON value to it's int representation.</summary>
public class JsonFloatToInt32Converter : JsonConverter<int>
{
    /// <inheritdoc/>
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        if (reader.TokenType == JsonTokenType.String) {
            if (int.TryParse(reader.GetString(), out var intValue)) {
                return intValue;
            }
        }
        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt32(out var intValue)) {
                return intValue;
            }
            else if (reader.TryGetDecimal(out var decimalValue)) {
                return (int) decimalValue;
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
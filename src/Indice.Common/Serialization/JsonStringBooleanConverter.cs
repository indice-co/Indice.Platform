using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's bool representation.</summary>
public class JsonStringBooleanConverter : JsonConverter<bool>
{
    /// <inheritdoc/>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        if (reader.TokenType == JsonTokenType.String) {
            if (bool.TryParse(reader.GetString(), out var booleanValue)) {
                return booleanValue;
            }
        }
        if (reader.TokenType == JsonTokenType.False) {
            return false;
        }
        if (reader.TokenType == JsonTokenType.True) {
            return true;
        }
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
}

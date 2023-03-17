using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's <see cref="TimeSpan"/> representation.</summary>
public class JsonTimeSpanConverter : JsonConverter<TimeSpan>
{
#if NET5_0_OR_GREATER
    /// <inheritdoc/>
    public override bool HandleNull => true;
#endif
    /// <inheritdoc/>
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        if (reader.TokenType == JsonTokenType.String) {
            if (TimeSpan.TryParse(reader.GetString(), out var timeSpanValue)) {
                return timeSpanValue;
            }
        }
        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt32(out var intValue)) {
                return TimeSpan.FromMinutes(intValue);
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}

/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's <see cref="TimeSpan"/> representation.</summary>
public class JsonNullableTimeSpanConverter : JsonConverter<TimeSpan?>
{
#if NET5_0_OR_GREATER
    /// <inheritdoc/>
    public override bool HandleNull => true;
#endif

    /// <inheritdoc/>
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        if (reader.TokenType == JsonTokenType.String) {
            if (TimeSpan.TryParse(reader.GetString(), out var timeSpanValue)) {
                return timeSpanValue;
            }
        }
        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt32(out var intValue)) {
                return TimeSpan.FromMinutes(intValue);
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options) {
        if (value.HasValue) {
            writer.WriteStringValue(value.Value.ToString());
        } else {
            writer.WriteNullValue();
        }
    }
}

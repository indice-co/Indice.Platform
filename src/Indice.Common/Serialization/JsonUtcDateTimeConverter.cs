using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;


/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's <see cref="TimeSpan"/> representation.</summary>
public class JsonUtcDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc/>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return GetDateTime(ref reader);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        WriteDateTime(writer, value);
    }

    internal static DateTime GetDateTime(ref Utf8JsonReader reader) {
        var date = reader.GetDateTime();
        switch (date.Kind) {
            case DateTimeKind.Unspecified:
                return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            case DateTimeKind.Local:
                return date.ToUniversalTime();
            case DateTimeKind.Utc:
            default:
                return date;
        }
    }
    internal static void WriteDateTime(Utf8JsonWriter writer, DateTime value) {
        switch (value.Kind) {
            case DateTimeKind.Unspecified:
                writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
                break;
            case DateTimeKind.Local:
                writer.WriteStringValue(value.ToUniversalTime());
                break;
            case DateTimeKind.Utc:
            default:
                writer.WriteStringValue(value);
                break;
        }
    }
}

/// <summary>A custom <see cref="JsonConverter"/> that tries to convert a string JSON value to it's <see cref="TimeSpan"/> representation.</summary>
public class JsonNullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc/>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return default;
        }
        if (reader.TokenType == JsonTokenType.String) {
            return JsonUtcDateTimeConverter.GetDateTime(ref reader);
        }
        return default;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) {
        if (value.HasValue) {
            JsonUtcDateTimeConverter.WriteDateTime(writer, value.Value);
        } else {
            writer.WriteNullValue();
        }
    }
}

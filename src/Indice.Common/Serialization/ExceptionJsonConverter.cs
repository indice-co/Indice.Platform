using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization;

/// <summary>
/// A JSON converter for <see cref="Exception"/> objects that allows optional inclusion
/// of stack traces and inner exceptions when serializing to JSON.
/// </summary>
/// <param name="IncludeStackTrace">
/// If <c>true</c>, the exception's stack trace and <see cref="Exception.Data"/> dictionary
/// will be included in the JSON output. Defaults to <c>false</c> to avoid exposing sensitive information.
/// </param>
/// <param name="IncludeInnerExceptions">
/// If <c>true</c>, inner exceptions (including <see cref="AggregateException.InnerExceptions"/>)
/// will be included in the JSON output. Defaults to <c>true</c> to provide detailed context about the failure.
/// </param>
public sealed class ExceptionJsonConverter(bool IncludeStackTrace, bool IncludeInnerExceptions) : JsonConverter<Exception>
{
    /// <summary>
    /// Deserialization is not supported.
    /// </summary>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException("Deserialization is not supported.");

    /// <summary>
    /// Writes an <see cref="Exception"/> object to JSON.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The exception to serialize.</param>
    /// <param name="options">The JSON serialization options.</param>
    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options) {
        static string Name(JsonSerializerOptions opts, string name) => opts.PropertyNamingPolicy?.ConvertName(name) ?? name;

        writer.WriteStartObject();
        writer.WriteString(Name(options, "type"), value.GetType().FullName);
        writer.WriteString(Name(options, "message"), value.Message);

        if (IncludeStackTrace) {
            writer.WriteString(Name(options, "stackTrace"), value.StackTrace ?? string.Empty);

            if (value.Data?.Count > 0) {
                writer.WritePropertyName(Name(options, "data"));
                writer.WriteStartObject();
                foreach (DictionaryEntry entry in value.Data) {
                    var key = entry.Key?.ToString() ?? "null";
                    var convertedKey = options.DictionaryKeyPolicy?.ConvertName(key) ?? key;
                    writer.WritePropertyName(convertedKey);
                    JsonSerializer.Serialize(writer, entry.Value, options);
                }
                writer.WriteEndObject();
            }
        }

        if (IncludeInnerExceptions) {
            if (value is AggregateException agg) {
                writer.WritePropertyName(Name(options, "innerExceptions"));
                JsonSerializer.Serialize(writer, agg.InnerExceptions, options);
            } else if (value.InnerException is not null) {
                writer.WritePropertyName(Name(options, "innerException"));
                JsonSerializer.Serialize(writer, value.InnerException, options);
            }
        }

        writer.WriteEndObject();
    }
}

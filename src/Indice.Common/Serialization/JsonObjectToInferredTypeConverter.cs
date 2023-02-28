using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Indice.Serialization;

/// <summary>A custom <see cref="JsonConverter"/> for scenarios that require type inference.</summary>
/// <remarks>https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to#deserialize-inferred-types-to-object-properties</remarks>
public class JsonObjectToInferredTypeConverter : JsonConverter<object>
{
    /// <inheritdoc/>
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.True) {
            return true;
        }
        if (reader.TokenType == JsonTokenType.False) {
            return false;
        }
        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt64(out var l)) {
                return l;
            }
            return reader.GetDouble();
        }
        if (reader.TokenType == JsonTokenType.String) {
            if (reader.TryGetDateTime(out var datetime)) {
                return datetime;
            }
            return reader.GetString();
        }
        using (var document = JsonDocument.ParseValue(ref reader)) {
            return document.RootElement.Clone();
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
        if (value?.GetType().Name is "JObject" or "JArray") {
            var json = value.ToString();
            var regex = new Regex(@"\:\s?(\d+\.0)\s?(,|}|\n)");
            json = regex.Replace(json, new MatchEvaluator(match => {
                return $":{match.Groups[1].Value.TrimEnd('0').TrimEnd('.')}{match.Groups[2].Value}";
            }));
            var document = JsonDocument.Parse(json);
            value = document.RootElement.Clone();
        }
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

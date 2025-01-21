using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Indice.Features.Cases.Core.Serialization;
/// <summary>
/// Converts a JsonNode to a JsonObject and vice versa for JSON serialization and deserialization.
/// </summary>

public class JsonNodeToJsonObjectAdapterConverter : JsonConverter<JsonNode>
{
    /// <summary>
    /// Reads JSON and converts it to a JsonNode.
    /// </summary>
    /// <param name="reader">The JsonReader to read from.</param>
    /// <param name="objectType">The type of the object.</param>
    /// <param name="existingValue">The existing value of the object being read.</param>
    /// <param name="hasExistingValue">Whether there is an existing value.</param>
    /// <param name="serializer">The JsonSerializer instance.</param>
    /// <returns>A JsonNode representation of the JSON data.</returns>
    public override JsonNode? ReadJson(JsonReader reader, Type objectType, JsonNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        try {
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }
            if (reader.TokenType == JsonToken.StartObject) {
                return JsonNode.Parse(JObject.Load(reader).ToString());
            }
            if (reader.TokenType == JsonToken.StartArray) {
                return JsonNode.Parse(JArray.Load(reader).ToString());
            }
            return reader.Value!.ToString();
        } catch (JsonReaderException) {
            return null;
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Writes a JsonNode to JSON.
    /// </summary>
    /// <param name="writer">The JsonWriter to write to.</param>
    /// <param name="value">The JsonNode value to write.</param>
    /// <param name="serializer">The JsonSerializer instance.</param>
    public override void WriteJson(JsonWriter writer, JsonNode? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
        } else {
            writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
        }
    }
}


/// <summary>
/// Converts a JsonNode to a JsonObject and vice versa for JSON serialization and deserialization.
/// </summary>
public class JsonNullableNodeToJsonObjectAdapterConverter : JsonConverter<JsonNode?>
{
    /// <summary>
    /// Reads JSON and converts it to a JsonNode.
    /// </summary>
    /// <param name="reader">The JsonReader to read from.</param>
    /// <param name="objectType">The type of the object.</param>
    /// <param name="existingValue">The existing value of the object being read.</param>
    /// <param name="hasExistingValue">Whether there is an existing value.</param>
    /// <param name="serializer">The JsonSerializer instance.</param>
    /// <returns>A JsonNode representation of the JSON data.</returns>
    public override JsonNode? ReadJson(JsonReader reader, Type objectType, JsonNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }
        if (reader.TokenType == JsonToken.StartObject) {
            return JsonNode.Parse(JObject.Load(reader).ToString());
        }
        if (reader.TokenType == JsonToken.StartArray) {
            return JsonNode.Parse(JArray.Load(reader).ToString());
        }
        return reader.Value!.ToString();
    }

    /// <summary>
    /// Writes a JsonNode to JSON.
    /// </summary>
    /// <param name="writer">The JsonWriter to write to.</param>
    /// <param name="value">The JsonNode value to write.</param>
    /// <param name="serializer">The JsonSerializer instance.</param>
    public override void WriteJson(JsonWriter writer, JsonNode? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
        } else {
            writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
        }
    }
}
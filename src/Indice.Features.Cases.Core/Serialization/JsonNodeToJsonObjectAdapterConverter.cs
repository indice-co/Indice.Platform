using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Core.Serialization;
/// <summary>
/// Converts a JsonNode to a JsonObject and vice versa for JSON serialization and deserialization.
/// </summary>

public class JsonNodeToJsonObjectAdapterConverter : JsonConverter<JsonNode>
{
    /// <inheritdoc />
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

    /// <inheritdoc />
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
    /// <inheritdoc />
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

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, JsonNode? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
        } else {
            writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
        }
    }
}
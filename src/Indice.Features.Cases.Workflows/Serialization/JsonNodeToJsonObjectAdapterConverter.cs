using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Workflows.Serialization;

/// <inheritdoc />
public class JsonNodeToJsonObjectAdapterConverter : JsonConverter<JsonNode>
{
    /// <inheritdoc />
    public override JsonNode? ReadJson(JsonReader reader, Type objectType, JsonNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        try {
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }
            //if (reader.TokenType == JsonToken.String && string.IsNullOrWhiteSpace(reader.ReadAsString())) {
            //    return null;
            //}
            if (reader.TokenType == JsonToken.StartObject) {
                return JsonNode.Parse(JObject.Load(reader).ToString());
            }
            if (reader.TokenType == JsonToken.StartArray) {
                return JsonNode.Parse(JArray.Load(reader).ToString());
            }
            return reader.Value!.ToString();
            //return (JsonNode)JToken.Load(reader).ToString();
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

/// <inheritdoc />
public class JsonNullableNodeToJsonObjectAdapterConverter : JsonConverter<JsonNode?>
{
    /// <inheritdoc />
    public override JsonNode? ReadJson(JsonReader reader, Type objectType, JsonNode? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }
        //if (reader.TokenType == JsonToken.String && string.IsNullOrWhiteSpace(reader.ReadAsString())) {
        //    return null;
        //}
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
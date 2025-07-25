using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Workflows.Serialization;

// todo: improve serialize/deserialize
/// <inheritdoc />
public class JTokenToJsonNodeConverter : JsonConverter<JToken>
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeof(JToken).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonNode = JsonNode.Parse(ref reader);
        return ConvertJsonNodeToJToken(jsonNode);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, JToken value, JsonSerializerOptions options)
    {
        var jsonNode = ConvertJTokenToJsonNode(value);
        jsonNode?.WriteTo(writer);
    }

    private static JToken ConvertJsonNodeToJToken(JsonNode? jsonNode)
    {
        if (jsonNode == null) return JValue.CreateNull();
        
        return jsonNode switch {
            JsonValue jsonValue => JToken.FromObject(jsonValue.GetValue<object>()),
            JsonArray jsonArray => new JArray(jsonArray.Select(ConvertJsonNodeToJToken)),
            JsonObject jsonObject => new JObject(
                jsonObject.ToDictionary<KeyValuePair<string, JsonNode?>, string, JToken>(
                    kv => kv.Key, 
                    kv => ConvertJsonNodeToJToken(kv.Value) ?? JValue.CreateNull()
                )
            ),
            _ => JValue.CreateNull()
        };
    }

    private static JsonNode? ConvertJTokenToJsonNode(JToken? jToken)
    {
        if (jToken == null) return null;

        return jToken switch {
            JValue jValue => JsonValue.Create(jValue.Value),
            JArray jArray => new JsonArray(jArray.Select(ConvertJTokenToJsonNode).ToArray()),
            JObject jObject => new JsonObject(
                jObject!.ToDictionary<KeyValuePair<string, JToken>, string, JsonNode?>(
                    kv => kv.Key,
                    kv => ConvertJTokenToJsonNode(kv.Value)
                )
            ),
            _ => null
        };
    }
}

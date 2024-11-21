using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Serialization;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Extensions;

internal static class ObjectExtensions
{
    /// <summary>
    /// Parse object into JsonNode.
    /// This respects the naming policy specified in the object.
    /// </summary>
    /// <returns><see cref="JsonNode"/> instance or null</returns>
    public static JsonNode ToJsonNode(this object json) {
        return json switch {
            JsonNode jsonNode => jsonNode,
            null => null,
            JsonElement element => JsonNode.Parse(element.GetRawText()),
            JObject jObject => JsonNode.Parse(jObject.ToString()),
            string text => JsonNode.Parse(text),
            _ => JsonNode.Parse(
                    JsonSerializer.Serialize(json,
                        JsonSerializerOptionDefaults.GetDefaultSettings().WithIgnorePropertyNamingPolicy()))
        };
    }
}
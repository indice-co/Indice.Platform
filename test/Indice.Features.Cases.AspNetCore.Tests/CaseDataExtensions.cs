using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.EntityFrameworkCore.ValueConversion;

namespace Indice.Features.Cases.Tests;

public static class CaseDataExtensions
{
    internal static JsonNode ToJsonNode(this string jsonString) {
        dynamic converted = new JsonStringValueConverter<dynamic>().ConvertFromProvider(jsonString);
        return JsonNode.Parse(((JsonElement)converted).GetRawText())!;
    }

    internal static string FromJsonNode(this JsonNode jsonNode) {
        dynamic converted = new JsonStringValueConverter<dynamic>().ConvertToProvider(jsonNode);
        return converted;
    }

    internal static string ThroughConverter(this string jsonString) {
        return jsonString.ToJsonNode().FromJsonNode();
    }
}
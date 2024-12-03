using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.EntityFrameworkCore.ValueConversion;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Tests;

public static class JsonMergeTestExtensions
{
    /// <summary>Simulates data fetching from the db.</summary>
    internal static JsonNode FromDb(this string jsonString) {
        dynamic converted = new JsonStringValueConverter<dynamic>().ConvertFromProvider(jsonString);
        return JsonNode.Parse(((JsonElement)converted).GetRawText())!;
    }

    /// <summary>Simulates data path from Object all the way through the wire to the api.</summary>
    internal static JsonNode ThroughHttp(this object mergeObj) {
        var httpClient = JsonSerializer.SerializeToNode(mergeObj);
        var wire = httpClient!.ToJsonString();
        var elsa = JToken.Parse(wire);
        return elsa.ParseAsJsonNode() ?? throw new ArgumentNullException(nameof(mergeObj));
    }

    /// <summary>Simulates data storing to the db.</summary>
    internal static string ToDb(this JsonNode jsonNode) {
        dynamic converted = new JsonStringValueConverter<dynamic>().ConvertToProvider(jsonNode);
        return converted;
    }

    /// <summary></summary>
    internal static string ThroughDb(this string jsonString) {
        return jsonString.FromDb().ToDb();
    }
}
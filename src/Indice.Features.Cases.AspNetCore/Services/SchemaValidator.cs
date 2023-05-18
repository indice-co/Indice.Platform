using System.Text.Json;
using Indice.Features.Cases.Interfaces;
using Indice.Serialization;
using Json.Schema;
using Newtonsoft.Json.Linq;

namespace Indice.Features.Cases.Services;

internal class SchemaValidator : ISchemaValidator
{
    public bool IsValid(string schema, object data) {
        if (string.IsNullOrEmpty(schema)) throw new ArgumentNullException(nameof(schema));
        if (data is null) throw new ArgumentNullException(nameof(data));

        var mySchema = JsonSchema.FromText(schema);
        var jsonElement = data switch {
            JsonElement element => element,
            JObject jObject => JsonDocument.Parse(jObject.ToString()).RootElement,
            string text => JsonDocument.Parse(text).RootElement,
            _ => JsonDocument.Parse(JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings())).RootElement
        };

        var validate = mySchema.Evaluate(jsonElement, new EvaluationOptions {
            OutputFormat = OutputFormat.List
        });

        return validate.IsValid;
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Serialization;
using Json.More;
using Json.Schema;

namespace Indice.Features.Cases.Core.Services;

/// <summary>
/// A system text implementation of the <see cref="ISchemaValidator"/> that respects Newtonsoft json objects as well
/// </summary>
public class CasesJsonSchemaValidator : ISchemaValidator
{
    /// <inheritdoc/>
    public bool IsValid(string schema, object data) {
        ArgumentException.ThrowIfNullOrEmpty(schema);
        ArgumentNullException.ThrowIfNull(data);

        var mySchema = JsonSchema.FromText(schema);
        var jsonNode = (data, data.GetType().Name) switch {
            (JsonElement element, _) => element.AsNode(),
            (JsonNode node, _) => node,
            (object jObject, "JObject") => JsonNode.Parse(jObject.ToString()!), // this is sparter because we do not require a reference to the Newtonsoft library
            (string text, _) => JsonNode.Parse(text),
            _ => JsonNode.Parse(JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()))
        };

        var validate = mySchema.Evaluate(jsonNode, new EvaluationOptions {
            OutputFormat = OutputFormat.List
        });

        return validate.IsValid;
    }
}
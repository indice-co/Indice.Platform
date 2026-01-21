using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Serialization;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>
/// Provides extension methods for converting the workflow data of a case to strongly typed objects or JSON
/// representations.
/// </summary>
/// <remarks>These methods enable convenient access to the underlying workflow data stored in a case, allowing
/// callers to deserialize or cast the data to the desired type. The extensions are intended for use with cases from the
/// Indice.Features.Cases.Workflows.Integrations namespace. Thread safety depends on the usage of the underlying case
/// object and its data.</remarks>
public static class CasesExtensions
{
    /// <summary>
    /// Convert the CaseWorkflowData (object) to TData.
    /// </summary>    
    public static TData CaseWorkflowDataAs<TData>(this Integrations.Case @case) {
        if (@case.Data is TData typedData) {
            return typedData;
        }

        var json = JsonSerializer.Serialize(@case.Data, JsonSerializerOptionDefaults.GetDefaultSettings());
        if (typeof(TData) == typeof(string)) {
            return (TData)(object)json;
        }
        return JsonSerializer.Deserialize<TData>(json, JsonSerializerOptionDefaults.GetDefaultSettings())!;
    }

    /// <summary>
    /// Convert the CaseWorkflowData (object) to JsonNode.
    /// </summary>    
    public static JsonNode? CaseWorkflowDataAsJsonNode(this Integrations.Case @case) {
        if (@case.Data is JsonElement jsonElement) {
            return JsonSerializer.SerializeToNode(jsonElement);
        }

        return @case.CaseWorkflowDataAs<JsonNode?>();
    }
}
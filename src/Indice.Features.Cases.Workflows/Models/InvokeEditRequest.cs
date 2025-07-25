using System.Text.Json.Nodes;
using Indice.Features.Cases.Workflows.Serialization;

namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Invoke Edit Request Model.</summary>
public class InvokeEditRequest
{
    /// <summary>Id of the case.</summary>
    public Guid CaseId { get; set; }
    
    /// <summary>The Data to edit the case with.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(JsonNodeToJsonObjectAdapterConverter))]
    public JsonNode? Data { get; set; }

    /// <summary>Comment added on the edit action.</summary>
    public string Comment { get; set; } = null!;
    
    /// <summary>The Actor.</summary>
    public Actor Actor { get; set; } = null!;
}
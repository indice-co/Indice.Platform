using System.Text.Json.Nodes;

namespace Indice.Features.Cases.Core.Models;

/// <summary>The request to update the data of the case.</summary>
public class UpdateCaseRequest
{
    /// <summary>The data in json string.</summary>
    public JsonNode Data { get; set; } = null!;
}
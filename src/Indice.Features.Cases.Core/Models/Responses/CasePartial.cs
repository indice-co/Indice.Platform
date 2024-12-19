using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Serialization;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The partial model of a case.</summary>
public class CasePartial
{
    /// <summary>The Id of the case.</summary>
    public Guid Id { get; set; }

    /// <summary>The reference number of this case if it has one.</summary>
    /// <remarks>To enable set to <see langword="true"/> the <strong>ReferenceNumberEnabled</strong> flag</remarks>
    public int? ReferenceNumber { get; set; }

    /// <summary>The Id of the customer/ or contact as provided from integration services (core or 3rd party).</summary>
    /// <remarks>Correlation reference.</remarks>
    public string? OwnerId { get; set; }

    /// <summary>The full name of the customer.</summary>
    public string? OwnerName { get; set; }

    /// <summary>The Id of the user as provided from our Identity server.</summary>
    public string? UserId { get; set; }

    /// <summary>The created date of the case.</summary>
    public DateTimeOffset? CreatedByWhen { get; set; }

    /// <summary>The Id of the user that created the case.</summary>
    public string? CreatedById { get; set; }

    /// <summary>The email of the user that created the case.</summary>
    public string? CreatedByEmail { get; set; }

    /// <summary>The full name of the user that created the case.</summary>
    public string? CreatedByName { get; set; }

    /// <summary>The <see cref="CaseType"/> of the case.</summary>
    public CaseTypePartial CaseType { get; set; } = null!;

    /// <summary>The case metadata as provided from the client or integrator.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>The Id of the group the case belongs.</summary>
    public string? GroupId { get; set; }

    /// <summary>The current checkpoint type for the case.</summary>
    public CheckpointType CheckpointType { get; set; } = null!;

    /// <summary>The json data of the case.</summary>
    public dynamic? Data { get; set; }

    /// <summary>The name of the user that has the case assigned.</summary>
    public string? AssignedToName { get; set; }

    /// <summary>The channel of th case.</summary>
    public string? Channel { get; set; }

    /// <summary>Indicate if the case is in draft mode.</summary>
    public bool Draft { get; set; }

    /// <summary>Indicates the access of the user on this case. For admin Users the access level is set to default 111</summary>
    public int AccessLevel { get; set; }

    /// <summary>Convert case data to typed version.</summary>
    public TData DataAs<TData>() {
        if (Data is TData typedData) {
            return typedData;
        }
        
        var json = JsonSerializer.Serialize(Data, JsonSerializerOptionDefaults.GetDefaultSettings());
        if (typeof(TData) == typeof(string)) {
            return json;
        }
        return JsonSerializer.Deserialize<TData>(json, JsonSerializerOptionDefaults.GetDefaultSettings());
    }

    /// <summary>Get Data as Json Node</summary>
    /// <returns></returns>
    public JsonNode? DataAsJsonNode() {
        if (Data is JsonElement jsonElement) {
            return JsonSerializer.SerializeToNode(jsonElement);
        }

        return DataAs<JsonNode>();
    }
}

using Indice.Features.Cases.Data.Models;
using Newtonsoft.Json.Converters;

namespace Indice.Features.Cases.Models.Responses;

/// <summary>
/// Checkpoint type
/// </summary>
public class GetCheckpointTypeResponse
{
    /// <summary>The Id of the <b>checkpoint type</b>.</summary>
    public Guid Id { get; set; }

    /// <summary>The code of the <b>checkpoint type</b>.</summary>
    public string Code { get; set; }

    /// <summary>The title of the <b>checkpoint type</b>.</summary>
    public string Title { get; set; }

    /// <summary>The description of the <b>checkpoint type</b>.</summary>
    public string Description { get; set; }

    /// <summary>The status of the case. This is the external status for the customer.</summary>
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))] // Because of Elsa.HttpActivities the default serializer is newtonsoft
    public CaseStatus? Status { get; set; }

    /// <summary>Indicates if the checkpoint type is private, which means not visible to the customer.</summary>
    public bool? Private { get; set; }

    /// <summary>The translations of the <b>checkpoint type</b>.</summary>
    public string Translations { get; set; }
}

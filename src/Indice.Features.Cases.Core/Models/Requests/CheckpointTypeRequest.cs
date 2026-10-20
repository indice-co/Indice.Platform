using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>The checkpoint type create/update request model.</summary>
public class CheckpointTypeRequest
{
    /// <summary>The Id of the case type the checkpoint type belongs to.</summary>
    public Guid CaseTypeId { get; set; }

    /// <summary>The code of the checkpoint type. Must be unique per case type.</summary>
    public string Code { get; set; } = null!;

    /// <summary>The title of the checkpoint type.</summary>
    public string? Title { get; set; }

    /// <summary>The description of the checkpoint type.</summary>
    public string? Description { get; set; }

    /// <summary>The external status of the case (visible to the customer).</summary>
    public CaseStatus Status { get; set; }

    /// <summary>Indicates if the checkpoint type is private, which means not visible to the customer.</summary>
    public bool Private { get; set; }

    /// <summary>The translations of the checkpoint type.</summary>
    public TranslationDictionary<CheckpointTypeTranslation>? Translations { get; set; }
}

namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Request to change the relation key of a case.</summary>
public class UpdateCaseRelationKeyRequest
{
    /// <summary>The new relation key. Empty dissolves the relation.</summary>
    public string? RelationKey { get; set; }
}

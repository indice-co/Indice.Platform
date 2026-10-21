namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Filter for the checkpoint types list.</summary>
public class GetCheckpointTypesListFilter
{
    /// <summary>Filter by the case type Id.</summary>
    public Guid? CaseTypeId { get; set; }

    /// <summary>When true, returns the distinct checkpoint types grouped by code (no paging).</summary>
    public bool? Distinct { get; set; }
}

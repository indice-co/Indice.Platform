namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Request to relate cases to a case.</summary>
public class AddRelatedCasesRequest
{
    /// <summary>The ids of the cases to relate.</summary>
    public List<Guid> CaseIds { get; set; } = [];
}

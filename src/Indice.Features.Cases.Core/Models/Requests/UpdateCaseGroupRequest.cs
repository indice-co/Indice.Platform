namespace Indice.Features.Cases.Core.Models.Requests;

/// <summary>Request to change the group of a case.</summary>
public class UpdateCaseGroupRequest
{
    /// <summary>The new group id. Empty clears it.</summary>
    public string? GroupId { get; set; }
}

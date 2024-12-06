namespace Indice.Features.Cases.Core.Models;

/// <summary>The approval request to trigger the <strong>Approval</strong> process</summary>
/// <remarks>This should relate to an ApprovalActivity on the workflow side of things</remarks>
public class ApprovalRequest 
{
    /// <summary>User action for approval.</summary>
    public Approval Action { get; set; }

    /// <summary>User comment related to the action.</summary>
    public string? Comment { get; set; }
}
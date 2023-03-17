using Indice.Features.Cases.Workflows.Activities;

namespace Indice.Features.Cases.Models;

/// <summary>The approval request to trigger the <see cref="AwaitApprovalActivity"/></summary>
public class ApprovalRequest 
{
    /// <summary>User action for approval.</summary>
    public Approval Action { get; set; }

    /// <summary>User comment related to the action.</summary>
    public string Comment { get; set; }
}
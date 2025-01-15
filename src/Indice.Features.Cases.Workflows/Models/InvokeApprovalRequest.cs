namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Invoke Approval Request Model.</summary>
public class InvokeApprovalRequest
{
    /// <summary>Id of the case.</summary>
    public Guid CaseId { get; set; }
    
    /// <summary>User action for approval.</summary>
    public WorkflowApproval Action { get; set; }

    /// <summary>User comment related to the action.</summary>
    public string? Comment { get; set; }
    
    /// <summary>The Actor.</summary>
    public Actor Actor { get; set; } = null!;
}

/// <summary>Workflow Approval Model.</summary>
public enum WorkflowApproval
{
    /// <summary>Approve action.</summary>
    Approve,

    /// <summary>Reject action.</summary>
    Reject
}
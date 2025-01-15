namespace Indice.Features.Cases.Workflows.Models;

internal class WorkflowSubmitApprovalRequest
{
    public Guid CaseId { get; set; }

    public List<string> Roles { get; set; } // todo: remove
    
    /// <summary>User action for approval.</summary>
    public Approval OutputAction { get; set; } // todo: refactor to bool Approve?

    /// <summary>User comment related to the action.</summary>
    public string? OutputComment { get; set; } // todo: rename to DisplayedComment
    
    public CasesUser CasesUser { get; set; }
}

internal enum Approval
{
    /// <summary>Approve action.</summary>
    Approve,

    /// <summary>Reject action.</summary>
    Reject
}
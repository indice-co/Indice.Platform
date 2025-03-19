using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Models;

/// <summary>WorkflowAddApprovalRequest</summary>
public class WorkflowAddApprovalRequest
{
    /// <summary>Approval Action.</summary>
    public Approval Action {get; set;}
    
    /// <summary>Approval Reason.</summary>
    public string? Reason {get; set;}
    
    /// <summary>Actor responsible for this action.</summary>
    public WorkflowActor WorkflowActor {get; set;}
}
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Server.Models;

public class WorkflowAddApprovalWithCommentRequest
{
    public Guid CaseId {get; set;}
    public Approval Action {get; set;}
    public string? Reason {get; set;}
    public CasesActor CasesActor {get; set;}
    public bool PrivateComment {get; set;}
}
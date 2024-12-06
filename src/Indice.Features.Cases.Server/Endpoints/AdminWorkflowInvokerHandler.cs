using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminWorkflowInvokerHandler
{
    public static Task<NoContent> SubmitApproval(Guid caseId, ApprovalRequest request) {
        throw new NotImplementedException();
    }

    public static Task<NoContent> AssignCase(Guid caseId, ClaimsPrincipal currentUser) {
        var input = new AwaitAssignmentInvokerInput {
            // Get the current user for self-assign
            // todo support admin assignments [in future user-story]
            User = AuditMeta.Create(currentUser)
        };
        throw new NotImplementedException();
    }

    public static Task<NoContent> EditCase(Guid caseId, EditCaseRequest request) {
        throw new NotImplementedException();
    }

    public static Task<NoContent> TriggerAction(Guid caseId, ActionRequest request) {
        throw new NotImplementedException();
    }
}

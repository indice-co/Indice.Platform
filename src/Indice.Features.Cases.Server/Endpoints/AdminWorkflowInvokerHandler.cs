using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminWorkflowInvokerHandler
{
    public static async Task<NoContent> SubmitApproval(Guid caseId, ApprovalRequest request, IAwaitApprovalInvoker approvalInvoker) {
        var executedWorkflow = await approvalInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot approve or reject case at this point.");
        }
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> AssignCase(Guid caseId, IAwaitAssignmentInvoker awaitAssignmentInvoker, IHttpContextAccessor httpContextAccessor) {
        if (httpContextAccessor.HttpContext == null) {
            throw new Exception("HttpContext is not available.");
        }
        var input = new AwaitAssignmentInvokerInput {
            // Get the current user for self-assign
            // todo support admin assignments [in future user-story]
            User = AuditMeta.Create(httpContextAccessor.HttpContext.User)
        };
        var executedWorkflow = await awaitAssignmentInvoker.ExecuteWorkflowsAsync(caseId, input);
        if (!executedWorkflow.Any()) {
            throw new Exception("Case is already assigned.");
        }
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> EditCase(Guid caseId, EditCaseRequest request, IAwaitEditInvoker awaitEditInvoker) {
        var executedWorkflow = await awaitEditInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot edit at this point.");
        }
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> TriggerAction(Guid caseId, ActionRequest request, IAwaitActionInvoker awaitActionInvoker) {
        var executedWorkflow = await awaitActionInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot perform this action at this point.");
        }
        return TypedResults.NoContent();
    }
}

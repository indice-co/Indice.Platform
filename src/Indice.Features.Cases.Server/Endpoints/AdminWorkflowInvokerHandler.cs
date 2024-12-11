using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminWorkflowInvokerHandler
{
    public static async Task<Results<NoContent, ProblemHttpResult>> SubmitApproval(Guid caseId, ApprovalRequest request, ICasesWorkflowManager workflowManager, ClaimsPrincipal user) {
        var result = await workflowManager.SubmitApprovalAsync(user, caseId, request);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> AssignCase(Guid caseId, ICasesWorkflowManager workflowManager, ClaimsPrincipal user) {
        var result = await workflowManager.AssignCaseAsync(user, caseId);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> EditCase(Guid caseId, EditCaseRequest request, ICasesWorkflowManager workflowManager, ClaimsPrincipal user) {
        var result = await workflowManager.EditCaseAsync(user, caseId, request);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> TriggerAction(Guid caseId, ActionRequest request, ICasesWorkflowManager workflowManager, ClaimsPrincipal user) {
        var result = await workflowManager.TriggerActionAsync(user, caseId, request);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }
}

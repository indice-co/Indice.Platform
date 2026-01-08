using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Integration;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminWorkflowInvokerHandlers
{

    public static async Task<Results<NoContent, ProblemHttpResult>> SetRules(
        string caseTypeCode,
        DecisionTable decisionTable,
        WorkflowHttpServiceClient workflowHttpServiceClient
    ) {
        
        var result = await workflowHttpServiceClient.SetDecisionRules(caseTypeCode, decisionTable);
        
        return result.Success ? TypedResults.NoContent() : TypedResults.Problem(detail: result.Message);
    }

    public static async Task<DecisionsResponse> GetDecisions(
        string caseTypeCode,
        WorkflowHttpServiceClient workflowHttpServiceClient
    ) {
        return await workflowHttpServiceClient.GetDecisionDefinitions(caseTypeCode);
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> SubmitApproval(
        Guid caseId,
        ApprovalRequest request,
        ICasesWorkflowManager workflowManager,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        ICaseApprovalService caseApprovalService,
        ICaseActionsService caseActionsService,
        IAuthorizationService authorizationService,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService
    ) {
        var bookmarks = await workflowManager.GetActionsByCaseId(caseId) as AvailableActions;
        var approvalBookmark = bookmarks?.ApprovalBookmarks.FirstOrDefault();
        if (approvalBookmark is null) {
            return TypedResults.Problem(detail: "There is no valid approval action for this case.");
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([approvalBookmark.Role]));
        if (!authorizationResult.Succeeded) {
            return TypedResults.Problem(detail: "You are not authorized to access this case.");
        }

        if (approvalBookmark.BlockPreviousApprover) {
            var lastApproval = await caseApprovalService.GetLastApproval(caseId);
            if (currentUser.FindSubjectId() == lastApproval?.CreatedBy.Id) {
                return TypedResults.Problem(detail: "You are not authorized to access this case.");
            }
        }

        var result = await workflowManager.InvokeApprovalAsync(currentUser.UserToActor(casesOptions.Value), caseId, request);
        return result.Success ? TypedResults.NoContent() : TypedResults.Problem(detail: result.Message);
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> AssignCase(
        Guid caseId,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService,
        IAdminCaseMessageService caseMessageService,
        IAuthorizationService authorizationService,
        ICasesWorkflowManager workflowManager
    ) {
        var bookmarks = await workflowManager.GetActionsByCaseId(caseId) as AvailableActions;
        var assignmentBookmark = bookmarks?.AssignmentBookmarks.FirstOrDefault();
        if (assignmentBookmark is null) {
            return TypedResults.Problem(detail: "There is no valid assign action for this case.");
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([assignmentBookmark.Role]));
        if (!authorizationResult.Succeeded) {
            return TypedResults.Problem(detail: "You are not authorized to access this case.");
        }

        var result = await workflowManager.InvokeAssignmentAsync(caseId, currentUser.UserToActor(casesOptions.Value));
        return result.Success ? TypedResults.NoContent() : TypedResults.Problem(detail: result.Message);
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> EditCase(
        Guid caseId,
        EditCaseRequest request,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        ICasesWorkflowManager workflowManager,
        IAdminCaseMessageService caseMessageService,
        IAuthorizationService authorizationService,
        CasesMessageDescriber casesMessageDescriber
    ) {
        request.Data = JsonSerializer.SerializeToNode(request.Data);

        var bookmarks = await workflowManager.GetActionsByCaseId(caseId) as AvailableActions;
        var editBookmark = bookmarks?.EditBookmarks.FirstOrDefault();
        if (editBookmark is null) {
            return TypedResults.Problem(detail: "There is no valid edit action for this case.");
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([editBookmark.Role]));
        if (!authorizationResult.Succeeded) {
            return TypedResults.Problem(detail: "You are not authorized to access this case.");
        }

        var result = await workflowManager.InvokeEditAsync(
            currentUser.UserToActor(casesOptions.Value),
            caseId,
            casesMessageDescriber.EditCaseComment(currentUser.FindDisplayName(), currentUser.FindFirstValue(BasicClaimTypes.Email)),
            request);
        return result.Success ? TypedResults.NoContent() : TypedResults.Problem(detail: result.Message);
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> TriggerAction(
        Guid caseId,
        ActionRequest request,
        ICasesWorkflowManager workflowManager,
        IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions
    ) {
        var bookmarks = await workflowManager.GetActionsByCaseId(caseId) as AvailableActions;
        var customBookmark = bookmarks?.CustomActions.FirstOrDefault();
        if (customBookmark is null) {
            return TypedResults.Problem(detail: "There is no valid custom action for this case.");
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([customBookmark.AllowedRole]));
        if (!authorizationResult.Succeeded) {
            return TypedResults.Problem(detail: "You are not authorized to access this case.");
        }

        var result = await workflowManager.TriggerActionAsync(currentUser.UserToActor(casesOptions.Value), caseId, request);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }
}

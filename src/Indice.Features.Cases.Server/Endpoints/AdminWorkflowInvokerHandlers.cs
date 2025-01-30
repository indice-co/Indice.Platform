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

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminWorkflowInvokerHandlers
{
    
    public static async Task<Results<NoContent, ProblemHttpResult>> AssignCase(
        Guid caseId,
        ClaimsPrincipal currentUser,
        IAdminCaseService adminCaseService,
        IAdminCaseMessageService caseMessageService,
        ICasesWorkflowManager workflowManager) {
        
        // todo: authorization
        
        // todo: maybe just let's not notify the workflow if assignment did not succeed
        var outcome = "Done";
        try {
            await adminCaseService.AssignCase(AuditMeta.Create(currentUser), caseId);
        } catch (Exception ex) {
            outcome = "Failed";
            await caseMessageService.Send(caseId, CasesClaimsPrincipalExtensions.SystemUser(), ex);
        }
        
        // todo: handle errors
        await workflowManager.AssignCaseAsync(currentUser, caseId, outcome);
        
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> SubmitApproval(
        Guid caseId,
        ApprovalRequest request,
        ICasesWorkflowManager workflowManager,
        ClaimsPrincipal currentUser,
        ICaseApprovalService caseApprovalService,
        ICaseActionsService caseActionsService,
        IAuthorizationService authorizationService,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService) {
        
        // todo: remove userRoles
        var userRoles = currentUser
            .FindAll(x => x.Type == BasicClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();
        userRoles.Add(string.Empty);

        var bookmarks = await workflowManager.GetActionsByCaseId(currentUser, caseId, userRoles.ToArray()) as AvailableActions;
        var approvalBookmark = bookmarks?.ApprovalBookmarks.FirstOrDefault();
        if (approvalBookmark is null) { // todo: check empty list
            return TypedResults.Problem(detail: "You are not authorized to access this case.");
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
        
        // todo: move to service
        await caseMessageService.Send(
            caseId,
            currentUser,
            new Message {
                Comment = caseSharedResourceService.GetLocalizedHtmlString(request.Comment ?? string.Empty).Value,
                PrivateComment = !approvalBookmark.PublicActions.Contains(request.Action.ToString())
            });
        await caseApprovalService.AddApproval(caseId, null, currentUser, request.Action, request.Comment);
        
        // var result = await workflowManager.SubmitApprovalAsync(currentUser, caseId, request);
        var result = await workflowManager.InvokeApprovalAsync(currentUser, caseId, request);
        // if (!result.Success) {
        //     return TypedResults.Problem(detail: result.Message);
        // }
        return TypedResults.NoContent(); 
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> EditCase(
        Guid caseId,
        EditCaseRequest request,
        ClaimsPrincipal currentUser,
        ICasesWorkflowManager workflowManager,
        IAdminCaseMessageService caseMessageService,
        CasesMessageDescriber casesMessageDescriber) {
        // var result = await workflowManager.EditCaseAsync(currentUser, caseId, request);
        // if (!result.Success) {
        //     return TypedResults.Problem(detail: result.Message);
        // }
        request.Data = JsonSerializer.SerializeToNode(request.Data);
        await caseMessageService.Send(caseId,
            currentUser,
            new Message {
                Data = request.Data,
                Comment = casesMessageDescriber.EditCaseComment(currentUser.FindDisplayName(), currentUser.FindFirstValue(BasicClaimTypes.Email)),
                PrivateComment = true
            });
        // var client = new CasesWorkflowManagerHttp();
        // var result = await client.EditCaseAsync(currentUser, caseId, request);

        var response = workflowManager.InvokeEditAsync(currentUser, caseId, request);
        
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> TriggerAction(Guid caseId, ActionRequest request, ICasesWorkflowManager workflowManager, ClaimsPrincipal currentUser) {
        var result = await workflowManager.TriggerActionAsync(currentUser, caseId, request);
        if (!result.Success) {
            return TypedResults.Problem(detail: result.Message);
        }
        return TypedResults.NoContent();
    }
}

using System.Security.Claims;
using System.Text.Json;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class WorkflowHandler
{
     public static async Task<Ok> RollbackApproval(
        Guid caseId,
        ClaimsPrincipal user,
        ICaseApprovalService caseApprovalService) {
        await caseApprovalService.RollbackApproval(caseId);
        return TypedResults.Ok();
    }
    
    public static async Task<Results<Ok<CaseApproval>, NoContent>> GetLastApproval(
        Guid caseId,
        ClaimsPrincipal user,
        ICaseApprovalService caseApprovalService) {
        var result = await caseApprovalService.GetLastApproval(caseId);
        return result is null ? TypedResults.NoContent() : TypedResults.Ok(result);
    }
    
    public static async Task<Results<Ok<Case>, NotFound>> GetWorkflowCaseById(
        Guid caseId,
        IAdminCaseService adminCareService,
        bool includeAttachments = false
    ) {
        var currentUser = CasesClaimsPrincipalExtensions.SystemUser(); // todo: client_credentials, add AuditMeta
        var @case = await adminCareService.GetCaseById(currentUser, caseId, includeAttachments);
        return @case is not null ? TypedResults.Ok(@case) : TypedResults.NotFound();
    }
    
    public static async Task<Ok> AddApproval(
        WorkflowAddApprovalRequest request,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        ICaseApprovalService caseApprovalService
    ) {
        var user = CasesClaimsPrincipalExtensions.SystemUser(); // todo: client_credentials, add AuditMeta
        await caseApprovalService.AddApproval(request.CaseId, null, user, request.Action, request.Reason, request.CasesActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Ok> AddApprovalWithComment(
        WorkflowAddApprovalWithCommentRequest request,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        ICaseApprovalService caseApprovalService
    ) {
        var user = CasesClaimsPrincipalExtensions.SystemUser(); // todo: client_credentials, add AuditMeta
        await caseMessageService.Send(request.CaseId, user, new Message {
            Comment = caseSharedResourceService.GetLocalizedHtmlString(request.Reason ?? string.Empty).Value,
            PrivateComment = request.PrivateComment
        });

        await caseApprovalService.AddApproval(request.CaseId, null, user, request.Action, request.Reason, request.CasesActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Ok<AuditMeta>> Assign(
        Guid caseId,
        CasesActor actor,
        IAdminCaseService adminCaseService) {
        var assignedTo = await adminCaseService.AssignCase(actor.ToAuditMeta(), caseId);
        return TypedResults.Ok(assignedTo);
    }
    
    public static async Task<Ok> RemoveAssignment(
        Guid caseId,
        ClaimsPrincipal user,
        IAdminCaseService adminCaseService) {
        await adminCaseService.RemoveAssignment(caseId);
        return TypedResults.Ok();
    }

    // TODO: JSON.parse(JSON.stringify(workflowExecutionContext.CurrentScope.Variables.Data.CurrentValue.Message)) in update-cases-recurring.json
    public static async Task<Ok> SendMessage(
        Guid caseId,
        WorkflowSendMessageRequest request,
        IAdminCaseMessageService adminCaseMessageService) {
        var user = CasesClaimsPrincipalExtensions.SystemUser(); // todo: client_credentials, add AuditMeta
        if (request.Message.Data is JsonElement) {
            request.Message.Data = JsonSerializer.SerializeToNode(request.Message.Data);
        }
        await adminCaseMessageService.Send(caseId, user, request.Message, request.CasesActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Results<Ok<Contact>, NotFound>> GetContactReference(
        ClaimsPrincipal currentUser,
        IContactProvider customerIntegrationService,
        string reference,
        string caseTypeCode) {
        var user = CasesClaimsPrincipalExtensions.SystemUser(); // todo: client_credentials, add AuditMeta
        var contactData = await customerIntegrationService.GetByReferenceAsync(user, reference, caseTypeCode);
        if (contactData == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(contactData);
    }
}
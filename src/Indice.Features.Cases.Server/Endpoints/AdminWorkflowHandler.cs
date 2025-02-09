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

internal static class AdminWorkflowHandler
{
     public static async Task<Ok> RollbackApproval(Guid caseId, ICaseApprovalService caseApprovalService) {
        await caseApprovalService.RollbackApproval(caseId);
        return TypedResults.Ok();
    }
    
    public static async Task<Results<Ok<CaseApproval>, NoContent>> GetLastApproval(Guid caseId, ICaseApprovalService caseApprovalService) {
        var result = await caseApprovalService.GetLastApproval(caseId);
        return result is null ? TypedResults.NoContent() : TypedResults.Ok(result);
    }
    
    public static async Task<Results<Ok<Case>, NotFound>> GetWorkflowCaseById(
        Guid caseId,
        ClaimsPrincipal currentUser,
        IAdminCaseService adminCareService,
        bool includeAttachments = false
    ) {
        var @case = await adminCareService.GetCaseById(currentUser, caseId, includeAttachments);
        return @case is not null ? TypedResults.Ok(@case) : TypedResults.NotFound();
    }
    
    public static async Task<Ok> AddApproval(
        WorkflowAddApprovalRequest request,
        IAdminCaseMessageService caseMessageService,
        ICaseApprovalService caseApprovalService
    ) {
        await caseApprovalService.AddApproval(request.CaseId, null, request.Action, request.Reason, request.WorkflowActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Ok> AddApprovalWithComment(
        WorkflowAddApprovalWithCommentRequest request,
        ClaimsPrincipal currentUser,
        IAdminCaseMessageService caseMessageService,
        CaseSharedResourceService caseSharedResourceService,
        ICaseApprovalService caseApprovalService
    ) {
        await caseMessageService.Send(request.CaseId, currentUser, new Message {
            Comment = caseSharedResourceService.GetLocalizedHtmlString(request.Reason ?? string.Empty).Value,
            PrivateComment = request.PrivateComment
        });

        await caseApprovalService.AddApproval(request.CaseId, null, request.Action, request.Reason, request.WorkflowActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Ok<AuditMeta>> Assign(Guid caseId, WorkflowActor actor, IAdminCaseService adminCaseService) {
        var assignedTo = await adminCaseService.AssignCase(actor.ToAuditMeta(), caseId);
        return TypedResults.Ok(assignedTo);
    }
    
    public static async Task<Ok> RemoveAssignment(Guid caseId, IAdminCaseService adminCaseService) {
        await adminCaseService.RemoveAssignment(caseId);
        return TypedResults.Ok();
    }
    
    public static async Task<Ok> BlockPreviousApprover(
        Guid caseId,
        WorkflowActor actor,
        ClaimsPrincipal currentUser,
        CasesMessageDescriber casesMessageDescriber,
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService
    ) {
        await caseMessageService.Send(caseId, currentUser, new Message {
            Comment = casesMessageDescriber.BlockPreviousApproverCommentWithCulture("en-US"), // todo: pass culture in body
            PrivateComment = true
        }, actor.ToAuditMeta());
        
        await adminCaseService.RemoveAssignment(caseId);
        
        return TypedResults.Ok();
    }

    // TODO: check json serialization with json from elsa JSON.parse(JSON.stringify(workflowExecutionContext.CurrentScope.Variables.Data.CurrentValue.Message)) in update-cases-recurring.json
    public static async Task<Ok> SendMessage(
        Guid caseId,
        WorkflowSendMessageRequest request,
        ClaimsPrincipal currentUser,
        IAdminCaseMessageService adminCaseMessageService
    ) {
        if (request.Message.Data is JsonElement) { // todo: fix that
            request.Message.Data = JsonSerializer.SerializeToNode(request.Message.Data);
        }
        await adminCaseMessageService.Send(caseId, currentUser, request.Message, request.WorkflowActor.ToAuditMeta());
        return TypedResults.Ok();
    }
    
    public static async Task<Results<Ok<Contact>, NotFound>> GetContactReference(
        string reference,
        string caseTypeCode,
        ClaimsPrincipal currentUser,
        IContactProvider customerIntegrationService
    ) {
        var contactData = await customerIntegrationService.GetByReferenceAsync(currentUser, reference, caseTypeCode);
        return contactData == null ? TypedResults.NotFound() : TypedResults.Ok(contactData);
    }
}
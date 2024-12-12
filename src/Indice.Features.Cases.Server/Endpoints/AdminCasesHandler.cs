using System.Net.Mime;
using System.Security.Claims;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

///<summary>Cases from the administrative perspective.</summary>
internal static class AdminCasesHandler
{
    public static async Task<Ok<Guid>> CreateDraftAdminCase(
        CreateDraftCaseRequest request,
        ClaimsPrincipal User,
        IAdminCaseService adminCaseService) =>
        TypedResults.Ok(await adminCaseService.CreateDraft(User, request.CaseTypeCode, request.GroupId, request.Customer, request.Metadata));

    public static async Task<Ok<ResultSet<CaseAttachment>>> GetCaseAttachments(Guid caseId, IAdminCaseService adminCaseService) =>
        TypedResults.Ok(await adminCaseService.GetAttachments(caseId));

    public static async Task<Results<Ok<CasesAttachmentLink>, ValidationProblem>> UploadAdminCaseAttachment(
        Guid caseId,
        ClaimsPrincipal user,
        IFormFile file,
        IAdminCaseMessageService adminCaseMessageService,
        IOptions<CasesOptions> options) {
        if (file.Length is 0) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(file), "File is empty."));
        }
        var attachmentId = await adminCaseMessageService.Send(caseId, user, new Message {
            FileName = file.FileName,
            FileStreamAccessor = () => file.OpenReadStream()
        });
        return TypedResults.Ok(new CasesAttachmentLink {
            Id = attachmentId.GetValueOrDefault()
        });
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetCaseAttachment(Guid caseId, Guid attachmentId, IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachment(caseId, attachmentId);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.File(attachment.Data!, attachment.ContentType, attachment.FileName);
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetAttachmentByField(Guid caseId, string fieldName, ClaimsPrincipal user, IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachmentByField(user, caseId, fieldName);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.File(attachment.Data!, attachment.ContentType, attachment.FileName);
    }

    public static async Task<Results<NoContent, NotFound>> UpdateAdminCase(Guid caseId, UpdateCaseRequest request, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        if (await adminCaseService.GetCaseById(user, caseId) is not { }) {
            return TypedResults.NotFound();
        }
        await adminCaseService.UpdateData(user, caseId, request.Data);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> SubmitAdminCase(Guid caseId, dynamic data, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        if (await adminCaseService.GetCaseById(user, caseId) is not { }) {
            return TypedResults.NotFound();
        }
        await adminCaseService.UpdateData(user, caseId, data);
        await adminCaseService.Submit(user, caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> PatchCaseMetadata(Guid caseId, Dictionary<string, string> metadata, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        var result = await adminCaseService.PatchCaseMetadata(caseId, user, metadata);
        if (!result) {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> AdminAddComment(Guid caseId, SendCommentRequest request, IAdminCaseMessageService adminCaseMessageService, ClaimsPrincipal user) {
        _ = await adminCaseMessageService.Send(caseId, user, new Message {
            Comment = request.Comment,
            PrivateComment = request.PrivateComment,
            ReplyToCommentId = request.ReplyToCommentId
        });
        return TypedResults.NoContent();
    }

    public static async Task<Ok<ResultSet<CasePartial>>> GetCases([AsParameters] ListOptions options, [AsParameters] GetCasesListFilter filter, IAdminCaseService adminCaseService, ClaimsPrincipal user) =>
        TypedResults.Ok(await adminCaseService.GetCases(user, ListOptions.Create(options, filter)));

    public static async Task<Results<Ok<Case>, NotFound>> GetCaseById(Guid caseId, IAdminCaseService adminCareService, ClaimsPrincipal user) {
        var @case = await adminCareService.GetCaseById(user, caseId, false);
        return @case is not null ? TypedResults.Ok(@case) : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteDraftCase(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        if (await adminCaseService.GetCaseById(user, caseId) is not { }) {
            return TypedResults.NotFound();
        }
        await adminCaseService.DeleteDraft(user, caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Ok<List<TimelineEntry>>> GetCaseTimeline(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        var timeline = await adminCaseService.GetTimeline(user, caseId);
        return TypedResults.Ok(timeline);
    }

    public static async Task<Ok<List<CasePartial>>> GetRelatedCases(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        var cases = await adminCaseService.GetRelatedCases(user, caseId);
        return TypedResults.Ok(cases);
    }

    public static async Task<Results<Ok<CaseActions>, NotFound>> GetCaseActions(Guid caseId, ICaseActionsService caseBookmarkService, ClaimsPrincipal user) {
        var actions = await caseBookmarkService.GetUserActions(user, caseId);
        return actions is null ? TypedResults.NotFound() : TypedResults.Ok(actions);
    }

    public static async Task<Ok<List<RejectReason>>> GetCaseRejectReasons(Guid caseId, ICaseApprovalService caseApprovalService, ClaimsPrincipal user) =>
        TypedResults.Ok(await caseApprovalService.GetRejectReasons(user, caseId));

    public static async Task<Results<Ok<FileContentHttpResult>, NotFound>> DownloadCasePdf(Guid caseId,
        ClaimsPrincipal user,
        IAdminCaseService adminCaseService,
        IPlatformEventService platformEventService,
        ICaseTemplateService caseTemplateService,
        ICasePdfService casePdfService) {
        var @case = await adminCaseService.GetCaseById(user, caseId, true);
        if (@case is null) {
            return TypedResults.NotFound();
        }
        var file = await CreatePdf(@case, caseTemplateService, casePdfService);
        var fileName = $"{@case.CaseType.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await platformEventService.Publish(new CaseDownloadedEvent(@case!, CasesCoreConstants.Channels.Agent));
        return TypedResults.Ok(TypedResults.File(file, MediaTypeNames.Application.Pdf, fileName));
    }

    private static async Task<byte[]> CreatePdf(Case @case, ICaseTemplateService caseTemplateService, ICasePdfService casePdfService) {
        var template = await caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }
}

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
        IAdminCaseService adminCaseService) {
        var result = await adminCaseService.CreateDraft(User, request.CaseTypeCode, request.GroupId, request.Customer, request.Metadata);
        return TypedResults.Ok(result);
    }

    public static async Task<Ok<ResultSet<CaseAttachment>>> GetCaseAttachments(Guid caseId, IAdminCaseService adminCaseService) {
        var attachments = await adminCaseService.GetAttachments(caseId);
        return TypedResults.Ok(attachments);
    }

    public static async Task<Results<Ok<CasesAttachmentLink>, ValidationProblem>> UploadAdminCaseAttachment(
        Guid caseId,
        ClaimsPrincipal User,
        IFormFile file,
        IAdminCaseMessageService adminCaseMessageService,
        IOptions<CasesOptions> options) {
        // Max file size is 6 MegaBytes
        if (file.Headers.ContentLength > 6291456) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(file), "File size is too big. Max file size allowed is 6MBs"));
        }
        if (file == null) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(file), "File is empty."));
        }
        var attachmentId = await adminCaseMessageService.Send(caseId, User, new Message { FileName = file.FileName, FileStreamAccessor = () => file.OpenReadStream() });
        return TypedResults.Ok(new CasesAttachmentLink { Id = attachmentId.GetValueOrDefault() });
    }

    public static async Task<Results<Ok<FileContentHttpResult>, NotFound>> GetCaseAttachment(Guid caseId, Guid attachmentId, IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachment(caseId, attachmentId);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(TypedResults.File(attachment.Data!, attachment.ContentType, attachment.FileName));
    }

    public static async Task<Results<NoContent, NotFound>> UpdateAdminCase(Guid caseId, UpdateCaseRequest request, IAdminCaseService adminCaseService, ClaimsPrincipal user) {
        if (adminCaseService.GetCaseById(user, caseId) == null) {
            return TypedResults.NotFound();
        }
        await adminCaseService.UpdateData(user, caseId, request.Data);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> SubmitAdminCase(Guid caseId, dynamic data, IAdminCaseService adminCaseService, ClaimsPrincipal User) {
        if (adminCaseService.GetCaseById(User, caseId) == null) {
            return TypedResults.NotFound();
        }
        await adminCaseService.UpdateData(User, caseId, data);
        await adminCaseService.Submit(User, caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Ok<ResultSet<CasePartial>>> GetCases([FromBody] ListOptions<GetCasesListFilter> options, IAdminCaseService adminCaseService, ClaimsPrincipal User) {
        var cases = await adminCaseService.GetCases(User, options);
        return TypedResults.Ok(cases);
    }

    public static async Task<Results<Ok<Case>, NotFound>> GetCaseById(Guid caseId, IAdminCaseService adminCareService, ClaimsPrincipal User) {
        var @case = await adminCareService.GetCaseById(User, caseId, false);
        if (@case is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(@case);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteDraftCase(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal User) {
        if (adminCaseService.GetCaseById(User, caseId) == null) {
            return TypedResults.NotFound();
        }
        await adminCaseService.DeleteDraft(User, caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<List<TimelineEntry>>, NotFound>> GetCaseTimeline(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal User) {
        var timeline = await adminCaseService.GetTimeline(User, caseId);
        return TypedResults.Ok(timeline);
    }

    public static async Task<Results<Ok<CaseActions>, NotFound>> GetCaseActions(Guid caseId, ICaseActionsService caseBookmarkService, ClaimsPrincipal User) {
        var actions = await caseBookmarkService.GetUserActions(User, caseId);
        if (actions is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(actions);
    }

    public static async Task<Ok<List<RejectReason>>> GetCaseRejectReasons(ClaimsPrincipal currentUser, Guid caseId, ICaseApprovalService caseApprovalService) {
        return TypedResults.Ok(await caseApprovalService.GetRejectReasons(currentUser, caseId));
    }

    public static async Task<Results<Ok<FileContentHttpResult>, NotFound>> DownloadCasePdf(Guid caseId,
        ClaimsPrincipal User,
        IAdminCaseService adminCaseService,
        IPlatformEventService platformEventService,
        ICaseTemplateService caseTemplateService,
        ICasePdfService casePdfService) {
        var @case = await adminCaseService.GetCaseById(User, caseId, true);
        if (@case is null) {
            return TypedResults.NotFound();
        }
        var file = await CreatePdf(@case, caseTemplateService, casePdfService);
        var fileName = $"{@case?.CaseType?.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await platformEventService.Publish(new CaseDownloadedEvent(@case!, CasesCoreConstants.Channels.Agent));
        return TypedResults.Ok(TypedResults.File(file, "application/pdf", fileName));
    }

    private static async Task<byte[]> CreatePdf(Case @case, ICaseTemplateService caseTemplateService, ICasePdfService casePdfService) {
        var template = await caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }
}
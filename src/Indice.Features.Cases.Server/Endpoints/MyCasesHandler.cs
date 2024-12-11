using System.Net.Mime;
using System.Security.Claims;
using Indice.Events;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Cases from the customer's perspective.</summary>
internal static class MyCasesHandler
{

    /// <summary>Get the list of the customer's cases.</summary>
    /// <param name="options">The ListOptions for filtering MyCases.</param>
    /// <param name="filter"></param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    /// <returns></returns>
    public static async Task<Ok<ResultSet<MyCasePartial>>> GetMyCases([AsParameters] ListOptions options, [AsParameters] GetMyCasesListFilter filter, ClaimsPrincipal user, IMyCaseService myCaseService) =>
        TypedResults.Ok(await myCaseService.GetCases(user, ListOptions.Create(options, filter)));

    /// <summary>Get case details by Id.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    public static async Task<Results<Ok<Case>, NotFound>> GetMyCaseById(Guid caseId, ClaimsPrincipal user, IMyCaseService myCaseService) {
        var @case = await myCaseService.GetCaseById(user, caseId);
        return @case is null ? TypedResults.NotFound() : TypedResults.Ok(@case);
    }

    /// <summary>Create a new case in draft mode. That means no one will be able to edit it besides the creator of the case.</summary>
    /// <param name="request">The draft.</param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    /// <returns></returns>
    public static async Task<Ok<CreateCaseResponse>> CreateDraftCase(CreateDraftCaseRequest request, ClaimsPrincipal user, IMyCaseService myCaseService) =>
        TypedResults.Ok(await myCaseService.CreateDraft(user, request.CaseTypeCode, request.GroupId, request.Customer, request.Metadata, request.Channel));

    /// <summary>Add an attachment to an existing case regardless of its status and mode (draft or not).</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="file">The file to attach.</param>
    /// <param name="user"></param>
    /// <param name="myCaseMessageService"></param>
    /// <returns></returns>
    public static async Task<Results<Ok<CasesAttachmentLink>, BadRequest>> UploadCaseAttachment(Guid caseId, IFormFile file, ClaimsPrincipal user, IMyCaseMessageService myCaseMessageService) {
        var attachmentId = await myCaseMessageService.Send(caseId, user, new Message { FileName = file.FileName, FileStreamAccessor = file.OpenReadStream });
        return attachmentId is null ? TypedResults.BadRequest() : TypedResults.Ok(new CasesAttachmentLink { Id = attachmentId.GetValueOrDefault() });
    }

    /// <summary>Update the case with the business data as defined at the specific case type</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The update request.</param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    /// <returns></returns>
    public static async Task<NoContent> UpdateCase(Guid caseId, UpdateCaseRequest request, ClaimsPrincipal user, IMyCaseService myCaseService) {
        await myCaseService.UpdateData(user, caseId, request.Data);
        return TypedResults.NoContent();
    }

    /// <summary>Submit the case by removing the draft mode.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    /// <returns></returns>
    public static async Task<NoContent> SubmitMyCase(Guid caseId, ClaimsPrincipal user, IMyCaseService myCaseService) {
        await myCaseService.Submit(user, caseId);
        return TypedResults.NoContent();
    }

    /// <summary>Download case in a PDF format</summary>
    /// <param name="caseId"></param>
    /// <param name="user"></param>
    /// <param name="myCaseService"></param>
    /// <param name="caseTemplateService"></param>
    /// <param name="casePdfService"></param>
    /// <param name="platformEventService"></param>
    public static async Task<FileContentHttpResult> DownloadMyCasePdf(Guid caseId, ClaimsPrincipal user, IMyCaseService myCaseService, ICaseTemplateService caseTemplateService, ICasePdfService casePdfService, IPlatformEventService platformEventService) {
        var @case = await myCaseService.GetCaseById(user, caseId);
        var file = await CreatePdf(@case!, caseTemplateService, casePdfService);
        var fileName = $"{@case!.CaseType.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await platformEventService.Publish(new CaseDownloadedEvent(@case, CasesApiConstants.Channels.Customer));
        return TypedResults.File(file, MediaTypeNames.Application.Pdf, fileName);
    }

    /// <summary>
    /// Helper method to create a PDF from a case.
    /// </summary>
    /// <param name="case"></param>
    /// <param name="caseTemplateService"></param>
    /// <param name="casePdfService"></param>
    /// <returns></returns>
    private static async Task<byte[]> CreatePdf(Case @case, ICaseTemplateService caseTemplateService, ICasePdfService casePdfService) {
        var template = await caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }
}

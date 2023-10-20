using System.Net.Mime;
using Indice.AspNetCore.Filters;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Cases from the customer's perspective.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.MyCasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesUser)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.MyCasesApiTemplatePrefixPlaceholder}/my/cases")]
internal class MyCasesController : ControllerBase
{
    public const string Name = "Cases";
    private readonly IMyCaseService _myCaseService;
    private readonly ICaseTemplateService _caseTemplateService;
    private readonly ICasePdfService _casePdfService;
    private readonly ICaseEventService _caseEventService;
    private readonly IMyCaseMessageService _caseMessageService;

    public MyCasesController(
        IMyCaseService myCaseService,
        ICaseTemplateService caseTemplateService,
        ICasePdfService casePdfService,
        IMyCaseMessageService caseMessageService,
        ICaseEventService caseEventService) {
        _myCaseService = myCaseService ?? throw new ArgumentNullException(nameof(myCaseService));
        _caseTemplateService = caseTemplateService ?? throw new ArgumentNullException(nameof(caseTemplateService));
        _casePdfService = casePdfService ?? throw new ArgumentNullException(nameof(casePdfService));
        _caseMessageService = caseMessageService ?? throw new ArgumentNullException(nameof(caseMessageService));
        _caseEventService = caseEventService ?? throw new ArgumentNullException(nameof(caseEventService));
    }

    /// <summary>Get the list of the customer's cases.</summary>
    /// <param name="options">The ListOptions for filtering MyCases.</param>
    /// <returns></returns>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<MyCasePartial>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetMyCases([FromQuery] ListOptions<GetMyCasesListFilter> options) {
        var cases = await _myCaseService.GetCases(User, options);
        return Ok(cases);
    }

    /// <summary>Get case details by Id.</summary>
    /// <param name="caseId">The Id of the case.</param>
    [ProducesResponseType(typeof(Case), 200)]
    [Produces(MediaTypeNames.Application.Json)]
    [HttpGet("{caseId:guid}")]
    public async Task<IActionResult> GetMyCaseById(Guid caseId) {
        var @case = await _myCaseService.GetCaseById(User, caseId);
        return Ok(@case);
    }

    /// <summary>Create a new case in draft mode. That means no one will be able to edit it besides the creator of the case.</summary>
    /// <param name="request">The draft.</param>
    /// <returns></returns>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateCaseResponse))]
    public async Task<IActionResult> CreateDraftCase([FromBody] CreateDraftCaseRequest request) {
        return Ok(await _myCaseService.CreateDraft(User, request.CaseTypeCode, request.GroupId, request.Customer, request.Metadata, request.Channel));
    }

    /// <summary>Add an attachment to an existing case regardless of its status and mode (draft or not).</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="file">The file to attach.</param>
    /// <returns></returns>
    [AllowedFileSize(6291456)] // 6 MegaBytes
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [HttpPost("{caseId:guid}/attachments")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CasesAttachmentLink))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
    public async Task<IActionResult> UploadCaseAttachment([FromRoute] Guid caseId, [FromForm] IFormFile file) {
        if (file == null) {
            ModelState.AddModelError(nameof(file), "File is empty.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var attachmentId = await _caseMessageService.Send(caseId, User, new Message { File = file });
        return Ok(new CasesAttachmentLink { Id = attachmentId.GetValueOrDefault() });
    }

    /// <summary>Update the case with the business data as defined at the specific case type</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The update request.</param>
    /// <returns></returns>
    [HttpPut("{caseId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateCase([FromRoute] Guid caseId, [FromBody] UpdateCaseRequest request) {
        await _myCaseService.UpdateData(User, caseId, request.Data);
        return NoContent();
    }

    /// <summary>Submit the case by removing the draft mode.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns></returns>
    [HttpPost("{caseId:guid}/submit")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitMyCase([FromRoute] Guid caseId) {
        await _myCaseService.Submit(User, caseId);
        return NoContent();
    }

    /// <summary>Download case in a PDF format</summary>
    /// <param name="caseId"></param>
    [ProducesResponseType(typeof(IFormFile), 200)]
    [Produces(MediaTypeNames.Application.Pdf, Type = typeof(IFormFile))]
    [HttpGet("{caseId:guid}/download")]
    public async Task<IActionResult> DownloadMyCasePdf(Guid caseId) {
        var @case = await _myCaseService.GetCaseById(User, caseId);
        var file = await CreatePdf(@case);
        var fileName = $"{@case.CaseType.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await _caseEventService.Publish(new CaseDownloadedEvent(@case, CasesApiConstants.Channels.Customer));
        return File(file, "application/pdf", fileName);
    }

    private async Task<byte[]> CreatePdf(Case @case) {
        var template = await _caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await _casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }
}

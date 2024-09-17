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
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Controllers;

/// /// <summary>Cases from the administrative perspective.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/cases")]
internal class AdminCasesController : ControllerBase
{
    private readonly IAdminCaseService _adminCaseService;
    private readonly ICasePdfService _casePdfService;
    private readonly ICaseTemplateService _caseTemplateService;
    private readonly ICaseActionsService _caseBookmarkService;
    private readonly IAdminCaseMessageService _adminCaseMessageService;
    private readonly ICaseApprovalService _caseApprovalService;
    private readonly ICaseEventService _caseEventService;
    private readonly AdminCasesApiOptions _options;

    public AdminCasesController(
        IAdminCaseService adminCaseService,
        ICasePdfService casePdfService,
        ICaseTemplateService caseTemplateService,
        ICaseActionsService caseBookmarkService,
        IAdminCaseMessageService adminCaseMessageService,
        ICaseApprovalService caseApprovalService,
        ICaseEventService caseEventService,
        IOptions<AdminCasesApiOptions> options) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        _casePdfService = casePdfService ?? throw new ArgumentNullException(nameof(casePdfService));
        _caseTemplateService = caseTemplateService ?? throw new ArgumentNullException(nameof(caseTemplateService));
        _caseBookmarkService = caseBookmarkService ?? throw new ArgumentNullException(nameof(caseBookmarkService));
        _adminCaseMessageService = adminCaseMessageService ?? throw new ArgumentNullException(nameof(adminCaseMessageService));
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        _caseEventService = caseEventService ?? throw new ArgumentNullException(nameof(caseEventService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Create a new case in draft mode.</summary>
    /// <param name="request">The draft.</param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IActionResult> CreateDraftAdminCase([FromBody] CreateDraftCaseRequest request) {
        return Ok(await _adminCaseService.CreateDraft(User, request.CaseTypeCode, request.GroupId, request.Customer, request.Metadata));
    }

    /// <summary>Get a list of Attachments for a CaseId</summary>
    /// <param name="caseId"></param>
    /// <returns></returns>
    [HttpGet("{caseId:guid}/attachments")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CaseAttachment>))]
    public async Task<IActionResult> GetCaseAttachments([FromRoute] Guid caseId) {
        var attachments = await _adminCaseService.GetAttachments(caseId);
        return Ok(attachments);
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
    public async Task<IActionResult> UploadAdminCaseAttachment([FromRoute] Guid caseId, IFormFile file) {
        if (file == null) {
            ModelState.AddModelError(nameof(file), "File is empty.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (!_options.PermittedAttachmentFileExtensions.Contains(fileExtension)) {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                { CasesApiConstants.ValidationErrorKeys.FileExtension, new[] { "File type extension is not acceptable." } }
            }));
        }
        var attachmentId = await _adminCaseMessageService.Send(caseId, User, new Message { File = file });
        return Ok(new CasesAttachmentLink { Id = attachmentId.GetValueOrDefault() });
    }

    /// <summary>Get a Case Attachment</summary>
    /// <param name="caseId"></param>
    /// <param name="attachmentId"></param>
    /// <returns></returns>
    [HttpGet("{caseId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
    [Produces(typeof(IFormFile))]
    public async Task<IActionResult> GetCaseAttachment([FromRoute] Guid caseId, [FromRoute] Guid attachmentId) {
        var attachment = await _adminCaseService.GetAttachment(caseId, attachmentId);
        if (attachment is null) {
            return NotFound();
        }
        return File(attachment.Data, attachment.ContentType, attachment.Name);
    }

    /// <summary>
    /// Get a Case Attachment by field name
    /// </summary>
    /// <param name="caseId"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    [HttpGet("{caseId:guid}/attachments/{attachmentName}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
    [Produces(typeof(IFormFile))]
    public async Task<IActionResult> GetAttachmentByField([FromRoute] Guid caseId, [FromRoute] string fieldName) {
        var attachment = await _adminCaseService.GetAttachmentByField(User, caseId, fieldName);
        if (attachment is null) {
            return NotFound();
        }
        return File(attachment.Data, attachment.ContentType, attachment.Name);
    }

    /// <summary>Update the case with the business data as defined at the specific case type. This action is allowed only for draft cases.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The update request.</param>
    /// <returns></returns>
    [HttpPut("{caseId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateAdminCase([FromRoute] Guid caseId, [FromBody] UpdateCaseRequest request) {
        await _adminCaseService.UpdateData(User, caseId, request.Data);
        return NoContent();
    }

    /// <summary>Submit the case by removing the draft mode.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="data">The data of the case.</param>
    /// <returns></returns>
    [HttpPost("{caseId:guid}/submit")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitAdminCase([FromRoute] Guid caseId, [FromBody] dynamic data) {
        await _adminCaseService.UpdateData(User, caseId, data);
        await _adminCaseService.Submit(User, caseId);
        return NoContent();
    }

    /// <summary>Patches the metadata of a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="metadata">The metadata to patch.</param>
    /// <returns></returns>
    [HttpPatch("{caseId:guid}/metadata")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
    public async Task<IActionResult> PatchCaseMetadata([FromRoute] Guid caseId, Dictionary<string, string> metadata) {
        if (metadata == null) {
            ModelState.AddModelError(nameof(metadata), "Metadata is empty.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var result = await _adminCaseService.PatchCaseMetadata(caseId, User, metadata);
        if (!result) {
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Add a comment to an existing case regardless of its status and mode (draft or not).
    /// </summary>
    /// <param name="caseId">The Id of the case</param>
    /// <param name="request">The message request</param>
    /// <returns></returns>
    [HttpPost("{caseId:guid}/comment")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
    public async Task<IActionResult> AdminAddComment([FromRoute] Guid caseId, [FromBody] SendCommentRequest request) {

        _ = await _adminCaseMessageService.Send(caseId, User, new Message { Comment = request.Comment, PrivateComment = request.PrivateComment, ReplyToCommentId = request.ReplyToCommentId });
        return NoContent();
    }


    /// <summary>Gets the list of all cases using the provided <see cref="ListOptions"/>.</summary>
    /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <response code="200">OK</response>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CasePartial>))]
    public async Task<IActionResult> GetCases([FromQuery] ListOptions<GetCasesListFilter> options) {
        var cases = await _adminCaseService.GetCases(User, options);
        return Ok(cases);
    }

    /// <summary>Gets a case with the specified id.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{caseId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Case))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetCaseById([FromRoute] Guid caseId) {
        var @case = await _adminCaseService.GetCaseById(User, caseId, false);
        if (@case is null) {
            return NotFound();
        }
        return Ok(@case);
    }

    /// <summary>Deletes a draft case.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [HttpDelete("{caseId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteDraftCase([FromRoute] Guid caseId) {
        await _adminCaseService.DeleteDraft(User, caseId);
        return NoContent();
    }

    /// <summary>Gets the timeline entries for a case.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{caseId:guid}/timeline")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TimelineEntry>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetCaseTimeline([FromRoute] Guid caseId) {
        var timeline = await _adminCaseService.GetTimeline(User, caseId);
        return Ok(timeline);
    }

    /// <summary>Gets the cases actions (Approval, edit, assignments, etc) for a case Id. Actions differ based on user role.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{caseId:guid}/actions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CaseActions))]
    public async Task<IActionResult> GetCaseActions([FromRoute] Guid caseId) {
        return Ok(await _caseBookmarkService.GetUserActions(HttpContext.User, caseId));
    }

    /// <summary>Get the reject reasons for a case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns></returns>
    [HttpGet("{caseId:guid}/reject-reasons")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<RejectReason>))]
    public async Task<IActionResult> GetCaseRejectReasons([FromRoute] Guid caseId) {
        return Ok(await _caseApprovalService.GetRejectReasons(caseId));
    }

    /// <summary>Download case in a PDF format.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [Produces(MediaTypeNames.Application.Pdf, Type = typeof(IFormFile))]
    [HttpGet("{caseId:guid}.pdf")]
    public async Task<IActionResult> DownloadCasePdf(Guid caseId) {
        var @case = await _adminCaseService.GetCaseById(User, caseId, true);
        if (@case is null) {
            return NotFound();
        }
        var file = await CreatePdf(@case);
        var fileName = $"{@case?.CaseType?.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await _caseEventService.Publish(new CaseDownloadedEvent(@case, CasesApiConstants.Channels.Agent));
        return File(file, "application/pdf", fileName);
    }

    private async Task<byte[]> CreatePdf(Case @case) {
        var template = await _caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await _casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }
}

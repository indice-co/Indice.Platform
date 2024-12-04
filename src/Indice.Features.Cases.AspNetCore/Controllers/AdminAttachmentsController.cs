using System.Net.Mime;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Manage attachments for a case.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/attachments")]
internal class AdminAttachmentsController : ControllerBase
{
    private readonly IAdminCaseService _adminCaseService;

    public AdminAttachmentsController(IAdminCaseService adminCaseService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    /// <summary>Download attachment in a PDF format for back-office users.</summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [Produces(MediaTypeNames.Application.Octet, Type = typeof(IFormFile))]
    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId) {
        var attachment = await _adminCaseService.GetAttachmentById(User, attachmentId);
        if (attachment is null) {
            return NotFound();
        }
        var fileName = $"{attachmentId}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.{attachment.FileExtension}";

        //filename will be accessible via the Content-Disposition response header, so remember to expose Content-Disposition in your Cors policy
        return File(attachment.Data!, attachment.ContentType!, fileName);
    }
}
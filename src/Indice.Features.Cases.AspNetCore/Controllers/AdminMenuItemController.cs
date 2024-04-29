using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Indice.Types;
using Indice.Features.Cases.Models.Responses;
using Indice.Features.Cases.Models;


namespace Indice.Features.Cases.Controllers;

/// <summary>Manage attachments for a case.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/menu-items")]
internal class AdminMenuItemController : ControllerBase
{
    private readonly ICaseTypeMenuService _caseTypeMenuService;

    public AdminMenuItemController(ICaseTypeMenuService caseTypeMenuService) {
        _caseTypeMenuService = caseTypeMenuService ?? throw new ArgumentNullException(nameof(caseTypeMenuService));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CaseTypeMenu>))]
    public async Task<IActionResult> GetMenuItems([FromQuery] ListOptions options) {
        var menuItems = await _caseTypeMenuService.GetMenuItems(options);
        return Ok(menuItems);
    }
}
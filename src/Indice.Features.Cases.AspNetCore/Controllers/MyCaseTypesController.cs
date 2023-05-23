using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Case types from the customer's perspective.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.MyCasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesUser)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.MyCasesApiTemplatePrefixPlaceholder}/my/case-types")]
internal class MyCaseTypesController : ControllerBase
{
    private readonly IMyCaseService _myCaseService;

    public MyCaseTypesController(IMyCaseService myCaseService) {
        _myCaseService = myCaseService ?? throw new ArgumentNullException(nameof(myCaseService));
    }

    /// <summary>Gets case types.</summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CaseTypePartial>))]
    [Produces(MediaTypeNames.Application.Json)]
    [HttpGet]
    public async Task<ActionResult> GetCaseTypes([FromQuery] ListOptions<GetMyCaseTypesListFilter> options) {
        var results = await _myCaseService.GetCaseTypes(options);
        return Ok(results);
    }

    /// <summary>Gets a case type by its code.</summary>
    /// <param name="caseTypeCode">The case type code.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CaseTypePartial))]
    [Produces(MediaTypeNames.Application.Json)]
    [HttpGet("{caseTypeCode}")]
    public async Task<ActionResult> GetCaseType(string caseTypeCode) {
        var results = await _myCaseService.GetCaseType(caseTypeCode);
        return Ok(results);
    }
}
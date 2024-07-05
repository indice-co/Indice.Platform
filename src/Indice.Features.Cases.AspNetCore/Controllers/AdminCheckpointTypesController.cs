using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Manage check point types.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/checkpoint-types")]

internal class AdminCheckpointTypesController : ControllerBase
{
    private readonly ICheckpointTypeService _checkpointTypeService;

    public AdminCheckpointTypesController(ICheckpointTypeService checkpointTypeService) {
        _checkpointTypeService = checkpointTypeService ?? throw new ArgumentNullException(nameof(checkpointTypeService));
    }

    /// <summary>Get the distinct checkpoint types grouped by code</summary>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CheckpointType>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetDistinctCheckpointTypes() =>
        Ok(await _checkpointTypeService.GetDistinctCheckpointTypes(User));

    /// <summary>Gets the distinct checkpoint types of the casetype specified</summary>
    [HttpGet("{caseTypeId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CheckpointType>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetCaseTypeCheckpointTypes(Guid caseTypeId) =>
        Ok(await _checkpointTypeService.GetCaseTypeCheckpointTypes(User, caseTypeId));

    /// <summary>Updates existing checkpoint types and creates new ones</summary>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CheckpointType))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> BulkUpdateCheckpointTypes([FromBody] List<CheckpointTypeRequest> checkpointTypeRequest) =>
        Ok(await _checkpointTypeService.BulkUpdateCheckpointTypes(checkpointTypeRequest));

    /// <summary>Creates a new checkpoint type</summary>
    [HttpPost("/create")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CheckpointType))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> CreateCheckpointType([FromBody] CheckpointTypeRequest checkpointTypeRequest) {
        try {
            var checkpointType = await _checkpointTypeService.CreateCheckpointType(checkpointTypeRequest);
            return Ok(checkpointType);
        } catch (Exception ex) {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = ex.Message });
        }
    }

    /// <summary>Edits a checkpoint type</summary>
    [HttpPost("/edit")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CheckpointType))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> EditCheckpointType([FromBody] EditCheckpointTypeRequest editCheckpointTypeRequest) =>
        Ok(await _checkpointTypeService.EditCheckpointType(editCheckpointTypeRequest));

    /// <summary>Edits a checkpoint type</summary>
    [HttpGet("/by-id")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetCheckpointTypeResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetCheckpointTypeById(Guid checkpointTypeId) =>
        Ok(await _checkpointTypeService.GetCheckpointTypeById(checkpointTypeId));
}
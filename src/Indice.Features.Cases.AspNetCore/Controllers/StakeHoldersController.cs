using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Features.Cases.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Case types from the administrative perspective.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/stakeholders")]
internal class StakeHoldersController : ControllerBase
{
    private readonly IStakeHolderService _stakeHolderService;

    public StakeHoldersController(IStakeHolderService stakeHolderService) {
        _stakeHolderService = stakeHolderService ?? throw new ArgumentNullException(nameof(stakeHolderService));
    }

    /// <summary>Add a new stakeholder.</summary>
    /// <param name="request">The draft.</param>
    /// <returns></returns>
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> AddStakeholderToCase([FromBody] StakeHolderRequest request) {
        await _stakeHolderService.Add(request);
        return NoContent();
    }

    /// <summary>Deletes a draft case.</summary>
    /// <param name="caseId">The id of the case.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [HttpDelete]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> RemoveStakeHolderFromCase([FromBody] StakeHolderDeleteRequest request) {
        await _stakeHolderService.Delete(request);
        return NoContent();
    }
    /// <summary>Gets the list of all stakeHolder <see cref="StakeHolder"/> for this case.</summary>
    /// <param name="caseId">The case id</param>
    /// <response code="200">OK</response>
    [HttpGet("{caseId:guid}/attachments")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StakeHolder>))]
    public async Task<IActionResult> GetCases([FromRoute] Guid caseId) {
        var cases = await _stakeHolderService.Get(caseId);
        return Ok(cases);
    }

    /// <summary>Update a specific Case Type.</summary>
    /// <param name="request">The new case type model.</param>
    [HttpPut("{caseTypeId:guid}")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateAccessLevel([FromBody] StakeHolderRequest request) {
        await _stakeHolderService.UpdateAccessLevel(request);
        return NoContent();
    }
}
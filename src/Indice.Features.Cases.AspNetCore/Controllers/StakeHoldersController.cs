using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
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


    //Add Stakeholder
    //Remove Stakeholder
    //List StakeHolders
    //update Accesslevel for case, stakeholder
    //Get Cases (New or add filter in cases)
}
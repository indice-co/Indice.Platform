using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Invoking workflow activities for suspended instances.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage")]
internal class AdminWorkflowInvokerController : ControllerBase
{
    private readonly ICasesWorkflowManager _workflowManager;

    public AdminWorkflowInvokerController(ICasesWorkflowManager workflowManager) {
        _workflowManager = workflowManager ?? throw new ArgumentNullException(nameof(workflowManager));
    }

    /// <summary>Invoke the approval activity to approve or reject the case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The approval request.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitApproval(Guid caseId, [FromBody] ApprovalRequest request) {
        var result = await _workflowManager.SubmitApprovalAsync(User, caseId, request);
        if (!result.Success) {
            return Problem(detail: result.Message);
        }
        return NoContent();
    }

    /// <summary>Invoke the assign activity to assign the case to the caller user.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> AssignCase(Guid caseId) {
        var result = await _workflowManager.AssignCaseAsync(User, caseId);
        if (!result.Success) {
            return Problem(detail: result.Message);
        }
        return NoContent();
    }

    /// <summary>Invoke the edit activity to edit the data of the case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The case data in json format.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> EditCase(Guid caseId, [FromBody] EditCaseRequest request) {
        var result = await _workflowManager.EditCaseAsync(User, caseId, request);
        if (!result.Success) {
            return Problem(detail: result.Message);
        }
        return NoContent();
    }

    /// <summary>Invoke the action activity to trigger a business action for the case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The action request.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/trigger-action")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> TriggerAction(Guid caseId, [FromBody] ActionRequest request) {
        var result = await _workflowManager.TriggerActionAsync(User, caseId, request);
        if (!result.Success) {
            return Problem(detail: result.Message);
        }
        return NoContent();
    }
}
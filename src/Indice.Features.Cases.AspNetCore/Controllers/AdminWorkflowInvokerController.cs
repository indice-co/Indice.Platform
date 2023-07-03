using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Indice.Features.Cases.Data.Models;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Services;

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
    private readonly IAwaitApprovalInvoker _approvalInvoker;
    private readonly IAwaitAssignmentInvoker _awaitAssignmentInvoker;
    private readonly IAwaitAssignmentVol2Invoker _awaitAssignmentVol2Invoker;
    private readonly IAwaitEditInvoker _awaitEditInvoker;
    private readonly IAwaitActionInvoker _awaitActionInvoker;

    public AdminWorkflowInvokerController(
        IAwaitApprovalInvoker approvalInvoker,
        IAwaitAssignmentInvoker awaitAssignmentInvoker,
        IAwaitAssignmentVol2Invoker awaitAssignmentVol2Invoker,
        IAwaitEditInvoker awaitEditInvoker,
        IAwaitActionInvoker awaitActionInvoker) {
        _approvalInvoker = approvalInvoker ?? throw new ArgumentNullException(nameof(approvalInvoker));
        _awaitAssignmentInvoker = awaitAssignmentInvoker ?? throw new ArgumentNullException(nameof(awaitAssignmentInvoker));
        _awaitAssignmentVol2Invoker = awaitAssignmentVol2Invoker ?? throw new ArgumentNullException(nameof(awaitAssignmentVol2Invoker));
        _awaitEditInvoker = awaitEditInvoker ?? throw new ArgumentNullException(nameof(awaitEditInvoker));
        _awaitActionInvoker = awaitActionInvoker ?? throw new ArgumentNullException(nameof(awaitActionInvoker));
    }

    /// <summary>Invoke the approval activity to approve or reject the case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The approval request.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitApproval(Guid caseId, [FromBody] ApprovalRequest request) {
        var executedWorkflow = await _approvalInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot approve or reject case at this point.");
        }
        return NoContent();
    }

    /// <summary>Invoke the assign activity to assign the case to the caller user.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="auditMeta">auditMeta.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("cases/{caseId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> AssignCase(Guid caseId, [FromBody] AuditMeta auditMeta) {
        var input = new AwaitAssignmentInvokerInput();
        IEnumerable<CollectedWorkflow> executedWorkflow;
        if (auditMeta == null) {
            input.User = AuditMeta.Create(HttpContext.User);
            executedWorkflow = await _awaitAssignmentInvoker.ExecuteWorkflowsAsync(caseId, input);

        } else {
            input.User = auditMeta;
            executedWorkflow = await _awaitAssignmentVol2Invoker.ExecuteWorkflowsAsync(caseId, input);
        }
        if (!executedWorkflow.Any()) {
            throw new Exception("Case is already assigned.");
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
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> EditCase(Guid caseId, [FromBody] EditCaseRequest request) {
        var executedWorkflow = await _awaitEditInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot edit at this point.");
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
        var executedWorkflow = await _awaitActionInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            throw new Exception("You cannot perform this action at this point.");
        }
        return NoContent();
    }
}
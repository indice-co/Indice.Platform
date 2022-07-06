using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage")]
    internal class AdminAssignmentsController: ControllerBase
    {
        private readonly IAwaitAssignmentInvoker _awaitAssignmentInvoker;

        public AdminAssignmentsController(IAwaitAssignmentInvoker awaitAssignmentInvoker) {
            _awaitAssignmentInvoker = awaitAssignmentInvoker ?? throw new ArgumentNullException(nameof(awaitAssignmentInvoker));
        }

        [HttpPost("cases/{caseId:guid}/assign")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> AssignCase(Guid caseId) {
            var input = new AwaitAssignmentInvokerInput {
                // Get the current user for self-assign
                // todo support admin assignments [in future user-story]
                User = AuditMeta.Create(HttpContext.User)
            };
            var executedWorkflow = await _awaitAssignmentInvoker.ExecuteWorkflowsAsync(caseId, input);
            if (!executedWorkflow.Any()) {
                throw new Exception ("Case is already assigned.");
            }
            // todo handle CanExecute
            // todo handle Exception
            return NoContent();
        }
    }
}
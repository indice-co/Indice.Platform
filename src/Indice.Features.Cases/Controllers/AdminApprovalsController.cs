using System;
using System.Linq;
using System.Threading.Tasks;
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
    internal class AdminApprovalsController: ControllerBase
    {
        private readonly IAwaitApprovalInvoker _approvalInvoker;

        public AdminApprovalsController(IAwaitApprovalInvoker approvalInvoker) {
            _approvalInvoker = approvalInvoker ?? throw new ArgumentNullException(nameof(approvalInvoker));
        }

        [HttpPost("cases/{caseId:guid}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> SubmitApproval(Guid caseId, [FromBody] ApprovalRequest evaluation) {
            var executedWorkflow = await _approvalInvoker.ExecuteWorkflowsAsync(caseId, evaluation);
            if (!executedWorkflow.Any()) {
                throw new Exception ("You cannot approve or reject case at this point.");
            }
            // todo handle CanExecute
            // todo handle Exception

            return NoContent();
        }
    }
}

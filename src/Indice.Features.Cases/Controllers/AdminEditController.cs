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
    internal class AdminEditController: ControllerBase
    {
        private readonly IAwaitEditInvoker _awaitEditInvoker;

        public AdminEditController( IAwaitEditInvoker awaitEditInvoker) {
            _awaitEditInvoker = awaitEditInvoker ?? throw new ArgumentNullException(nameof(awaitEditInvoker));
        }

        [HttpPost("cases/{caseId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> EditCase(Guid caseId, [FromBody] EditCaseRequest request) {
            var executedWorkflow = await _awaitEditInvoker.ExecuteWorkflowsAsync(caseId, request.Data);
            if (!executedWorkflow.Any()) {
                throw new Exception ("You cannot edit at this point.");
            }
            // todo handle CanExecute
            // todo handle Exception
            return NoContent();
        }
    }
}
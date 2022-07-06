using System;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
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
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/users")]
    internal class AdminUsersController : ControllerBase
    {
        private readonly IAdminCaseService _adminCaseService;

        public AdminUsersController(IAdminCaseService adminCaseService) {
            _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        }
        
        [HttpPost("subscriptions")]
        public async Task<IActionResult> CreateCaseTypeNotificationSubscription() {
            await _adminCaseService.CreateCaseTypeNotificationSubscription(User);
            return NoContent();
        }
    }
}
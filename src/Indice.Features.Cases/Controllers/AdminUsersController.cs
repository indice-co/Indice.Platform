using System;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage user and user options inside the case management.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/users")]
    internal class AdminUsersController : ControllerBase
    {
        private readonly ICaseTypeNotificationSubscriptionService _service;

        public AdminUsersController(ICaseTypeNotificationSubscriptionService service) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Get the case type subscriptions of a user.
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscriptions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CaseTypeSubscription))]
        public async Task<IActionResult> GetCaseTypeNotificationSubscription() {
            var subscribed = await _service.GetCaseTypeNotificationSubscriptionByUser(User);
            return Ok(new CaseTypeSubscription {
                Subscribed = subscribed
            });
        }

        /// <summary>
        /// Create new case type subscription for a user.
        /// </summary>
        /// <returns></returns>
        [HttpPost("subscriptions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> CreateCaseTypeNotificationSubscription() {
            await _service.CreateCaseTypeNotificationSubscription(User);
            return NoContent();
        }

        /// <summary>
        /// Remove a case type subscription for a user.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("subscriptions")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteCaseTypeNotificationSubscription() {
            await _service.DeleteCaseTypeNotificationSubscriptionByUser(User);
            return NoContent();
        }
    }
}
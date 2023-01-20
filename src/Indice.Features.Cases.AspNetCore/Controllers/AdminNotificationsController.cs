using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
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
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/my/notifications")]
    internal class AdminNotificationsController : ControllerBase
    {
        private readonly INotificationSubscriptionService _service;
        private readonly CasesApiOptions _casesApiOptions;

        public AdminNotificationsController(
            INotificationSubscriptionService service,
            CasesApiOptions casesApiOptions) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _casesApiOptions = casesApiOptions ?? throw new ArgumentNullException(nameof(casesApiOptions));
        }

        /// <summary>
        /// Get the case type subscriptions of a user.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NotificationSubscriptionResult))]
        public async Task<IActionResult> GetMySubscriptions() {
            var options = new ListOptions<NotificationFilter> {
                Filter = NotificationFilter.FromUser(User, _casesApiOptions.GroupIdClaimType)
            };
            var subscribed = await _service.GetSubscriptions(options);
            return Ok(new NotificationSubscriptionResult {
                Subscribed = subscribed
            });
        }

        /// <summary>
        /// Create new case type subscription for a user.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Subscribe() {
            await _service.Subscribe(NotificationSubscription.FromUser(User, _casesApiOptions.GroupIdClaimType));
            return NoContent();
        }

        /// <summary>
        /// Remove a case type subscription for a user.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Unsubscribe() {
            await _service.Unsubscribe(NotificationFilter.FromUser(User, _casesApiOptions.GroupIdClaimType));
            return NoContent();
        }
    }
}
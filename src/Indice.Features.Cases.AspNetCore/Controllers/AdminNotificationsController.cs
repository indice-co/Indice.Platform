using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Manage Notifications for Back-office users.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroup)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiPrefix}/manage/my/notifications")]
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

    /// <summary>Get the notification subscriptions for a user.</summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NotificationSubscriptionResponse))]
    public async Task<IActionResult> GetMySubscriptions() {
        var options = new ListOptions<NotificationFilter> {
            Filter = NotificationFilter.FromUser(User, _casesApiOptions.GroupIdClaimType)
        };
        var result = await _service.GetSubscriptions(options);
        return Ok(new NotificationSubscriptionResponse {
            NotificationSubscriptions = result
        });
    }

    /// <summary>Store user's subscription settings.</summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Subscribe(NotificationSubscriptionRequest request) {
        await _service.Subscribe(request.CaseTypeIds, NotificationSubscription.FromUser(User, _casesApiOptions.GroupIdClaimType));
        return NoContent();
    }
}
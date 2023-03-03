using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Identity.Api;

/// <summary>A controller responsible for sending push notifications.</summary>
/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[Route("api/devices")]
[ApiController]
[ApiExplorerSettings(GroupName = "identity", IgnoreApi = false)]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
[Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeAdmin)]
internal class PushNotificationsController : ControllerBase
{
    /// <summary>The name of the controller.</summary>
    public const string Name = "PushNotifications";

    public PushNotificationsController(IPushNotificationService pushNotificationService) {
        PushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
    }

    public IPushNotificationService PushNotificationService { get; }

    /// <summary>Sends a push notification.</summary>
    /// <param name="request">Contains information about the push notification to send.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpPost("push")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    public async Task<IActionResult> SendPushNotification([FromBody] SendPushNotificationRequest request) {
        if (request.Broadcast) {
            await PushNotificationService.BroadcastAsync(request.Title, request.Body, request.Data, request.Classification);
        } else {
            await PushNotificationService.SendToUserAsync(request.Title, request.Body, request.Data, request.UserTag, request.Classification, request.Tags ?? Array.Empty<string>());
        }
        return NoContent();
    }
}

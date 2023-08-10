using System.Net.Mime;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Features.Totp.Models;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity.Features;

/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
/// <response code="500">Internal Server Error</response>
[Route("totp")]
[ApiController]
[Authorize(Policy = IdentityServerApi.Scope)]
[ApiExplorerSettings(GroupName = IdentityServerApi.Scope)]
[Produces(MediaTypeNames.Application.Json)]
internal class TotpController : ControllerBase
{
    public TotpController(
        TotpServiceFactory totpServiceFactory,
        IStringLocalizer<TotpController> localizer
    ) {
        TotpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public TotpServiceFactory TotpServiceFactory { get; }
    public IStringLocalizer<TotpController> Localizer { get; }

    /// <summary>Sends a new code via the selected channel.</summary>
    /// <param name="request">The request object.</param>
    [HttpPost]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> Send(TotpRequest request) {
        var userId = User.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return Forbid();
        }
        var result = default(TotpResult);
        var totpService = TotpServiceFactory.Create<User>();
        switch (request.Channel) {
            case TotpDeliveryChannel.Sms:
            case TotpDeliveryChannel.Viber:
                result = await totpService.SendAsync(totp =>
                    totp.ToPrincipal(User)
                        .WithMessage(request.Message)
                        .UsingDeliveryChannel(request.Channel)
                        .WithPurpose(request.Purpose)
                );
                break;
            case TotpDeliveryChannel.PushNotification:
                result = await totpService.SendAsync(totp => totp
                    .ToPrincipal(User)
                    .WithMessage(request.Message)
                    .UsingPushNotification()
                    .WithSubject(request.Subject)
                    .WithPurpose(request.Purpose)
                    .WithData(request.Data)
                    .WithClassification(request.Classification)
                );
                break;
            case TotpDeliveryChannel.Email:
            case TotpDeliveryChannel.Telephone:
            default:
                return StatusCode(405);
        }
        if (!result.Success) {
            ModelState.AddModelError(nameof(request.Channel), result.Error ?? "An error occurred.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        return NoContent();
    }

    /// <summary>Verify the code received.</summary>
    /// <param name="request">The request object.</param>
    [HttpPut]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> Verify(TotpVerificationRequest request) {
        var userId = User.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return Forbid();
        }
        var result = await TotpServiceFactory.Create<User>().VerifyAsync(User, request.Code, request.Purpose);
        if (!result.Success) {
            ModelState.AddModelError(nameof(request.Code), Localizer["Invalid code"]);
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        return NoContent();
    }
}

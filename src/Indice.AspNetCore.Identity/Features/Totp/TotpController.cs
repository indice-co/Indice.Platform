using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Features.Totp.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity.Features
{
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
            ITotpService totpService,
            IStringLocalizer<TotpController> localizer
        ) {
            TotpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public ITotpService TotpService { get; }
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
            switch (request.Channel) {
                case TotpDeliveryChannel.Sms:
                    result = await TotpService.Send(options => options.UsePrincipal(User).WithMessage(request.Message).UsingSms().WithPurpose(request.Purpose));
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Error ?? "An error occurred.");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.Viber:
                    result = await TotpService.Send(options => options.UsePrincipal(User).WithMessage(request.Message).UsingViber().WithPurpose(request.Purpose));
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Error ?? "An error occurred.");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.PushNotification:
                    result = await TotpService.Send(options => options
                        .UsePrincipal(User)
                        .WithMessage(request.Message)
                        .UsingPushNotification()
                        .WithPurpose(request.Purpose)
                        .WithData(request.Data)
                        .WithClassification(request.Classification));
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Error ?? "An error occurred.");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.Email:
                case TotpDeliveryChannel.Telephone:
                default:
                    return StatusCode(405);
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
            var result = await TotpService.Verify(User, request.Code, request.Provider, request.Purpose);
            if (!result.Success) {
                ModelState.AddModelError(nameof(request.Code), Localizer["Invalid code"]);
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            return NoContent();
        }
    }
}

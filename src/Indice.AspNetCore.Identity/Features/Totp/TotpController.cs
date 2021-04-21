using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity.Features
{
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not found</response>
    /// <response code="405">Method Not Allowed</response>
    /// <response code="406">Not Acceptable</response>
    /// <response code="408">Request Timeout</response>
    /// <response code="409">Conflict</response>
    /// <response code="415">Unsupported Media Type</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="503">Service Unavailable</response>
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

        /// <summary>
        /// Sends a new code via the selected channel.
        /// </summary>
        /// <param name="request"></param>
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
                        ModelState.AddModelError(nameof(request.Channel), result.Errors.FirstOrDefault() ?? "An error occured.");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.Viber:
                    result = await TotpService.Send(options => options.UsePrincipal(User).WithMessage(request.Message).UsingViber().WithPurpose(request.Purpose));
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Errors.FirstOrDefault() ?? "An error occured.");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.PushNotification:
                    result = await TotpService.Send(options => options.UsePrincipal(User).WithMessage(request.Message).UsingPushNotification().WithPurpose(request.Purpose));
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Errors.FirstOrDefault() ?? "An error occured.");
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

        /// <summary>
        /// Verify the code received.
        /// </summary>
        /// <param name="request"></param>
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

    /// <summary>
    /// Request object used by an authenticated user in order to get a new Time base one time access token via one of the supported MFA mechanisms
    /// </summary>
    public class TotpRequest
    {
        /// <summary>
        /// Delivery channel. 
        /// </summary>
        [Required]
        public TotpDeliveryChannel Channel { get; set; }
        /// <summary>
        /// Optionaly pass the reason to generate the TOTP.
        /// </summary>
        public string Purpose { get; set; }
        /// <summary>
        /// The message to be sent in the SMS. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Verification request object.
    /// </summary>
    public class TotpVerificationRequest
    {
        /// <summary>
        /// The Totp code.
        /// </summary>
        [Required]
        public string Code { get; set; }
        /// <summary>
        /// Optionaly pass the provider to use to verify. Defaults to DefaultPhoneProvider.
        /// </summary>
        public TotpProviderType? Provider { get; set; }
        /// <summary>
        /// Optionaly pass the reason used to generate the TOTP.
        /// </summary>
        public string Purpose { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.AspNetCore.Identity.Features
{
    [Route("totp")]
    [ApiController]
    [Authorize(Policy = IdentityServerApi.Scope)]
    [ApiExplorerSettings(GroupName = IdentityServerApi.Scope)]
    [Produces("application/json")]
    internal class TotpController : ControllerBase
    {
        public TotpController(ITotpService totpService, IStringLocalizer<TotpController> localizer) {
            TotpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public ITotpService TotpService { get; }
        public IStringLocalizer<TotpController> Localizer { get; }

        /// <summary>
        /// Sends a new code via the selected channel.
        /// </summary>
        /// <param name="request"></param>
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
        [HttpPost]
        [ProducesResponseType(statusCode: 204, type: typeof(void))]
        [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemDetails))]
        public async Task<IActionResult> Send(TotpRequest request) {
            var userId = User.FindSubjectId();
            if (string.IsNullOrEmpty(userId)) {
                return Forbid();
            }
            switch (request.Channel) {
                case TotpDeliveryChannel.Sms:
                    var result = await TotpService.Send(options => 
                        options.UsePrincipal(User)
                               .WithMessage(request.Message)
                               .UsingSms()
                               .WithPurpose(request.Purpose)
                    );
                    if (!result.Success) {
                        ModelState.AddModelError(nameof(request.Channel), result.Errors.FirstOrDefault() ?? "an error occured");
                        return BadRequest(new ValidationProblemDetails(ModelState));
                    }
                    break;
                case TotpDeliveryChannel.Email:
                case TotpDeliveryChannel.Telephone:
                case TotpDeliveryChannel.Viber:
                default:
                    return StatusCode(405);
            }
            return NoContent();
        }

        /// <summary>
        /// Verify the code received.
        /// </summary>
        /// <param name="request"></param>
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
        [HttpPut]
        [ProducesResponseType(statusCode: 204, type: typeof(void))]
        [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: 404, type: typeof(ProblemDetails))]
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
        /// The TOTP code.
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

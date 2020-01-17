using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing a user's account.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/account")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
    [ProblemDetailsExceptionFilter]
    internal class AccountController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly EmailVerificationOptions _userEmailVerificationOptions;
        private readonly ChangeEmailOptions _changeEmailOptions;
        private readonly ChangePhoneNumberOptions _changePhoneNumberOptions;
        private readonly GeneralSettings _generalSettings;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Account";

        public AccountController(ExtendedUserManager<User> userManager, IOptions<GeneralSettings> generalSettings, EmailVerificationOptions userEmailVerificationOptions = null, ChangeEmailOptions changeEmailOptions = null,
            ChangePhoneNumberOptions changePhoneNumberOptions = null, IEmailService emailService = null, ISmsService smsService = null) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _userEmailVerificationOptions = userEmailVerificationOptions;
            _changeEmailOptions = changeEmailOptions;
            _changePhoneNumberOptions = changePhoneNumberOptions;
            _emailService = emailService;
            _smsService = smsService;
        }

        /// <summary>
        /// Requests an email change for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the email.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-email")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangeEmail([FromBody]UpdateUserEmailRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(request), new string[] { $"User already has email '{request.Email}'." } }
                }));
            }
            var result = await _userManager.SetEmailAsync(user, request.Email);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            await SendEmailConfirmation(user);
            return Ok();
        }

        /// <summary>
        /// Confirms the email address of a given user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="code">The confirmation token of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="302">Redirect</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [HttpGet("change-email/confirm")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status302Found, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmChangeEmail([FromQuery]string userId, [FromQuery]string code) => await ConfirmEmailInternal(userId, code, _changeEmailOptions?.ReturnUrl);

        /// <summary>
        /// Requests a phone number change for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the phone number.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-phone")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangePhoneNumber([FromBody]UpdateUserPhoneRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentPhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(request), new string[] { $"User already has phone number '{request.PhoneNumber}'." } }
                }));
            }
            var result = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            if (_changePhoneNumberOptions == null) {
                return Ok();
            }
            if (_smsService == null) {
                throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered. Check {nameof(ServiceCollectionExtensions.AddSmsServiceYouboto)} extension on {nameof(IServiceCollection)} or provide " +
                    $"your own implementation.");
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber);
            var message = _changePhoneNumberOptions.Message.Replace("{code}", code);
            await _smsService.SendAsync(request.PhoneNumber, _changePhoneNumberOptions.Subject, message);
            return Ok();
        }

        /// <summary>
        /// Confirms the phone number of the user, using the OTP.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="token">The OTP.</param>
        [AllowAnonymous]
        [HttpGet("change-phone/confirm")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmChangePhone([FromQuery]string userId, [FromQuery]string token) {
            if (string.IsNullOrEmpty(userId)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(userId), new string[] { $"Query parameter {nameof(userId)} is missing." } }
                }));
            }
            if (string.IsNullOrEmpty(token)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(token), new string[] { $"Query parameter {nameof(token)} is missing." } }
                }));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, token);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Changes the username for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the username.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-username")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangeUserName([FromBody]ChangeUserNameRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.SetUserNameAsync(user, request.UserName);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Changes the password for the current user, but requires the old password to be present.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Update the password expiration policy.
        /// </summary>
        /// <param name="request">Contains info about the chosen expiration policy.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-password-policy")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePasswordExpirationPolicy([FromBody]UpdatePasswordExpirationPolicyRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            user.PasswordExpirationPolicy = request.Policy;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        /// <summary>
        /// Confirms the email address of a given user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="code">The confirmation token of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="302">Redirect</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [HttpGet("confirm-email")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmEmail([FromQuery]string userId, [FromQuery]string code) => await ConfirmEmailInternal(userId, code, _userEmailVerificationOptions?.ReturnUrl);

        private async Task<IActionResult> ConfirmEmailInternal(string userId, string code, string returnUrl = null) {
            if (string.IsNullOrEmpty(userId)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(userId), new string[] { $"Query parameter {nameof(userId)} is missing." } }
                }));
            }
            if (string.IsNullOrEmpty(code)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(code), new string[] { $"Query parameter {nameof(code)} is missing." } }
                }));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            // TODO: Maybe add some more checks about the ReturnUrl option.
            if (!string.IsNullOrEmpty(returnUrl)) {
                return Redirect(returnUrl);
            }
            return Ok();
        }

        private async Task SendEmailConfirmation(User user) {
            if (_changeEmailOptions == null) {
                return;
            }
            if (_emailService == null) {
                throw new Exception($"No concrete implementation of {nameof(IEmailService)} is registered. Check {nameof(ServiceCollectionExtensions.AddEmailService)}, {nameof(ServiceCollectionExtensions.AddEmailServiceSmtpRazor)} or " +
                    $"{nameof(ServiceCollectionExtensions.AddEmailServiceSparkpost)} extensions on {nameof(IServiceCollection)} or provide your own implementation.");
            }
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{_generalSettings.Host}{Url.Action(nameof(ConfirmChangeEmail), Name, new { userId = user.Id, code })}";
            var recipient = user.Email;
            var subject = _changeEmailOptions.Subject;
            var body = _changeEmailOptions.Body.Replace("{callbackUrl}", callbackUrl);
            var data = new User {
                UserName = User.FindDisplayName() ?? user.UserName
            };
            await _emailService.SendAsync<User>(message => message.To(recipient).WithSubject(subject).WithBody(body).WithData(data));
        }
    }
}

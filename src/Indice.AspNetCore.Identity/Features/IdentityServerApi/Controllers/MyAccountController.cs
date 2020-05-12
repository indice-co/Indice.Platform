using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Route("api")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Scope)]
    [ProblemDetailsExceptionFilter]
    internal class MyAccountController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly GeneralSettings _generalSettings;
        private readonly IdentityOptions _identityOptions;
        private readonly IdentityServerApiEndpointsOptions _identityServerApiEndpointsOptions;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IEventService _eventService;
        private readonly IList<string> _errorCodes = new List<string> {
            nameof(IdentityErrorDescriber.PasswordTooShort),
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
            nameof(IdentityErrorDescriber.PasswordRequiresDigit),
            nameof(IdentityErrorDescriber.PasswordRequiresLower),
            nameof(IdentityErrorDescriber.PasswordRequiresUpper),
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars)
        };
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "MyAccount";

        public MyAccountController(ExtendedUserManager<User> userManager, IOptions<GeneralSettings> generalSettings, IOptionsSnapshot<IdentityOptions> identityOptions,
            IdentityServerApiEndpointsOptions identityServerApiEndpointsOptions, IEventService eventService, ISmsService smsService, IEmailService emailService) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
            _identityServerApiEndpointsOptions = identityServerApiEndpointsOptions ?? throw new ArgumentNullException(nameof(identityServerApiEndpointsOptions));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _smsService = smsService;
            _emailService = emailService;
        }

        /// <summary>
        /// Updates the email of the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the email address.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/email")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateEmail([FromBody]UpdateUserEmailRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase)) {
                ModelState.AddModelError(nameof(request.Email).ToLower(), $"User already has email '{request.Email}'.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.SetEmailAsync(user, request.Email);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            if (!_identityServerApiEndpointsOptions.Email.SendEmailOnUpdate) {
                return NoContent();
            }
            if (_emailService == null) {
                var message = $"No concrete implementation of {nameof(IEmailService)} is registered. " +
                              $"Check {nameof(ServiceCollectionExtensions.AddEmailService)}, {nameof(ServiceCollectionExtensions.AddEmailServiceSmtpRazor)} or " +
                              $"{nameof(ServiceCollectionExtensions.AddEmailServiceSparkpost)} extensions on {nameof(IServiceCollection)} or provide your own implementation.";
                throw new Exception(message);
            }
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var subject = _identityServerApiEndpointsOptions.Email.Subject;
            var body = _identityServerApiEndpointsOptions.Email.Body.Replace("{callbackUrl}", $"{request.ReturnUrl}{(request.ReturnUrl.Contains("?") ? "&" : "?")}userId={user.Id}&token={token}");
            var data = new User {
                UserName = User.FindDisplayName() ?? user.UserName
            };
            await _emailService.SendAsync<User>(message => message.To(user.Email).WithSubject(subject).WithBody(body).WithData(data));
            return NoContent();
        }

        /// <summary>
        /// Confirms the email address of a given user.
        /// </summary>
        /// <param name="request"></param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/email/confirmation")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmEmail([FromBody]ConfirmEmailRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users
                                         .Include(x => x.Claims)
                                         .Where(x => x.Id == userId)
                                         .SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.EmailConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), "User's email is already confirmed.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            var eventInfo = user.ToBasicUserInfo();
            await _eventService.Raise(new UserEmailConfirmedEvent(eventInfo));
            return NoContent();
        }

        /// <summary>
        /// Requests a phone number change for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the phone number.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/phone-number")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePhoneNumber([FromBody]UpdateUserPhoneNumberRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentPhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase)) {
                ModelState.AddModelError(nameof(request.PhoneNumber).ToLower(), $"User already has phone number '{request.PhoneNumber}'.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            if (!_identityServerApiEndpointsOptions.PhoneNumber.SendOtpOnUpdate) {
                return NoContent();
            }
            if (_smsService == null) {
                var message = $"No concrete implementation of {nameof(ISmsService)} is registered. " +
                              $"Check {nameof(ServiceCollectionExtensions.AddSmsServiceYouboto)} extension on {nameof(IServiceCollection)} or provide your own implementation.";
                throw new Exception(message);
            }
            var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber);
            var smsMessage = _identityServerApiEndpointsOptions.PhoneNumber.Message.Replace("{token}", token);
            await _smsService.SendAsync(request.PhoneNumber, string.Empty, smsMessage);
            return NoContent();
        }

        /// <summary>
        /// Confirms the phone number of the user, using the OTP token.
        /// </summary>
        /// <param name="request"></param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/phone-number/confirmation")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmPhoneNumber([FromBody]ConfirmPhoneNumberRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users
                                         .Include(x => x.Claims)
                                         .Where(x => x.Id == userId)
                                         .SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.PhoneNumberConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), "User's phone number is already confirmed.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, request.Token);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            var eventInfo = user.ToBasicUserInfo();
            await _eventService.Raise(new UserPhoneNumberConfirmedEvent(eventInfo));
            return NoContent();
        }

        /// <summary>
        /// Changes the username for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the username.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/username")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateUserName([FromBody]UpdateUserNameRequest request) {
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
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/password")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePassword([FromBody]ChangePasswordRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return NoContent();
        }

        /// <summary>
        /// Update the password expiration policy.
        /// </summary>
        /// <param name="request">Contains info about the chosen expiration policy.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/password-expiration-policy")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePasswordExpirationPolicy([FromBody]UpdatePasswordExpirationPolicyRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            user.PasswordExpirationPolicy = request.Policy;
            await _userManager.UpdateAsync(user);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes current user's account.
        /// </summary>
        /// <response code="204">No Content</response>
        [HttpDelete("my/account")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        public async Task<IActionResult> DeleteAccount() {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            await _userManager.DeleteAsync(currentUser);
            return NoContent();
        }

        /// <summary>
        /// Gets the password options that are applied when the user creates an account.
        /// </summary>
        /// <response code="200">OK</response>
        [AllowAnonymous]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
        [HttpGet("account/password-options")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(PasswordOptions))]
        public IActionResult GetPasswordOptions() => Ok(_identityOptions.Password);

        /// <summary>
        /// Checks if a username already exists in the database.
        /// </summary>
        /// <response code="302">Found</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [HttpPost("account/username-exists")]
        [ProducesResponseType(statusCode: StatusCodes.Status302Found, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(void))]
        public async Task<IActionResult> CheckUserNameExists([FromBody]ValidateUserNameRequest request) {
            if (!ModelState.IsValid) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var user = await _userManager.FindByNameAsync(request.UserName);
            return user == null ? NotFound() : StatusCode(StatusCodes.Status302Found);
        }

        /// <summary>
        /// Validates a user's password against one or more configured <see cref="IPasswordValidator{TUser}"/>.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("account/validate-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CredentialsValidationInfo))]
        public async Task<IActionResult> ValidatePassword([FromBody]ValidatePasswordRequest request) {
            if (!ModelState.IsValid) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var userNameWasProvided = !string.IsNullOrWhiteSpace(request.UserName);
            var response = new CredentialsValidationInfo {
                PasswordRules = new List<PasswordRuleInfo>()
            };
            foreach (var validator in _userManager.PasswordValidators) {
                var errorCode = GetErrorCode(validator, userNameWasProvided);
                if (errorCode != null) {
                    _errorCodes.Add(errorCode);
                }
                var result = await validator.ValidateAsync(_userManager, new User(request.UserName ?? string.Empty), request.Password);
                if (!result.Succeeded) {
                    foreach (var error in result.Errors) {
                        response.PasswordRules.Add(new PasswordRuleInfo {
                            Name = error.Code,
                            Description = error.Description,
                            IsValid = false
                        });
                    }
                }
            }
            foreach (var errorCode in _errorCodes) {
                var isContained = response.PasswordRules.SingleOrDefault(rule => rule.Name == errorCode) != null;
                if (!isContained) {
                    response.PasswordRules.Add(new PasswordRuleInfo {
                        Name = errorCode,
                        IsValid = true
                    });
                }
            }
            return Ok(response);
        }

        private string GetErrorCode(IPasswordValidator<User> validator, bool userNameWasProvided) {
            var validatorType = validator.GetType();
            validatorType = validatorType.IsGenericType ? validatorType.GetGenericTypeDefinition() : validatorType;
            if (validatorType.IsAssignableFrom(typeof(NonCommonPasswordValidator<>))) {
                return NonCommonPasswordValidator.ErrorDescriber;
            }
            if (validatorType.IsAssignableFrom(typeof(UserNameAsPasswordValidator)) && userNameWasProvided) {
                return UserNameAsPasswordValidator.ErrorDescriber;
            }
            return default;
        }
    }
}

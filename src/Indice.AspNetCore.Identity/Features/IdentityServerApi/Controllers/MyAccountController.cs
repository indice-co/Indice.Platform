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
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly GeneralSettings _generalSettings;
        private readonly IdentityOptions _identityOptions;
        private readonly IdentityServerApiEndpointsOptions _identityServerApiEndpointsOptions;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IEventService _eventService;
        private readonly ISmsServiceFactory _smsServiceFactory;
        private readonly MessageDescriber _messageDescriber;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "MyAccount";

        public MyAccountController(ExtendedUserManager<User> userManager, IOptions<GeneralSettings> generalSettings, IOptionsSnapshot<IdentityOptions> identityOptions,
            IdentityServerApiEndpointsOptions identityServerApiEndpointsOptions, IEventService eventService, ISmsServiceFactory smsServiceFactory, IEmailService emailService,
            MessageDescriber messageDescriber, ExtendedIdentityDbContext<User, Role> dbContext) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
            _identityServerApiEndpointsOptions = identityServerApiEndpointsOptions ?? throw new ArgumentNullException(nameof(identityServerApiEndpointsOptions));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
            _messageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _smsService = _smsServiceFactory.Create("Sms");
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
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateUserEmailRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase)) {
                ModelState.AddModelError(nameof(request.Email).ToLower(), _messageDescriber.EmailAlreadyExists(request.Email));
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
            var data = new User {
                UserName = User.FindDisplayName() ?? user.UserName
            };
            await _emailService.SendAsync<User>(message =>
                message.To(user.Email)
                       .WithSubject(_messageDescriber.EmailUpdateMessageSubject)
                       .WithBody(_messageDescriber.EmailUpdateMessageBody(request.ReturnUrl, user, token))
                       .WithData(data));
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
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users
                                         .Include(x => x.Claims)
                                         .Where(x => x.Id == userId)
                                         .SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.EmailConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), _messageDescriber.EmailAlreadyConfirmed);
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
        public async Task<IActionResult> UpdatePhoneNumber([FromBody] UpdateUserPhoneNumberRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentPhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase)) {
                ModelState.AddModelError(nameof(request.PhoneNumber).ToLower(), _messageDescriber.UserAlreadyHasPhoneNumber(request.PhoneNumber));
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
            await _smsService.SendAsync(request.PhoneNumber, string.Empty, _messageDescriber.PhoneNumberVerificationMessage(token));
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
        public async Task<IActionResult> ConfirmPhoneNumber([FromBody] ConfirmPhoneNumberRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users
                                         .Include(x => x.Claims)
                                         .Where(x => x.Id == userId)
                                         .SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.PhoneNumberConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), _messageDescriber.PhoneNumberAlreadyConfirmed);
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
        public async Task<IActionResult> UpdateUserName([FromBody] UpdateUserNameRequest request) {
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
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordRequest request) {
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
        public async Task<IActionResult> UpdatePasswordExpirationPolicy([FromBody] UpdatePasswordExpirationPolicyRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            await _userManager.SetPasswordExpirationPolicyAsync(user, request.Policy);
            return NoContent();
        }

        /// <summary>
        /// Assigns various properties on the current user's account.
        /// </summary>
        /// <param name="claims">Contains info about the claims to create.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPost("my/account/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<ClaimInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> AddClaims([FromBody] IEnumerable<CreateClaimRequest> claims) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var allowedClaims = await _dbContext.ClaimTypes
                                                .Where(x => claims.Select(x => x.Type).Contains(x.Name) && x.UserEditable)
                                                .Select(x => x.Name)
                                                .ToListAsync();
            if (allowedClaims.Count() != claims.Count()) {
                var notAllowedClaims = claims.Select(x => x.Type).Except(allowedClaims);
                ModelState.AddModelError(nameof(claims), $"The following claims are not allowed to add: '{string.Join(", ", notAllowedClaims)}'.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var claimsToAdd = claims.Select(x => new IdentityUserClaim<string> {
                UserId = user.Id,
                ClaimType = x.Type,
                ClaimValue = x.Value
            })
            .ToArray();
            _dbContext.UserClaims.AddRange(claimsToAdd);
            await _dbContext.SaveChangesAsync();
            return Ok(claimsToAdd.Select(x => new ClaimInfo {
                Id = x.Id,
                Type = x.ClaimType,
                Value = x.ClaimValue
            }));
        }

        /// <summary>
        /// Updates the specified claim for the current user.
        /// </summary>
        /// <param name="claimId">The id of the user claim.</param>
        /// <param name="request">Contains info about the claims to update.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/claims/{claimId:int}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateClaim([FromRoute] int claimId, [FromBody] UpdateUserClaimRequest request) {
            var userId = User.FindSubjectId();
            var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
            if (userClaim == null) {
                return NotFound();
            }
            var claimType = await _dbContext.ClaimTypes.SingleOrDefaultAsync(x => x.Name == userClaim.ClaimType);
            if (claimType == null) {
                return NotFound();
            }
            if (!claimType.UserEditable) {
                ModelState.AddModelError(nameof(claimType), $"Claim '{claimType.Name}' is not editable.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            userClaim.ClaimValue = request.ClaimValue;
            await _dbContext.SaveChangesAsync();
            return Ok(new ClaimInfo {
                Id = userClaim.Id,
                Type = userClaim.ClaimType,
                Value = request.ClaimValue
            });
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
        public async Task<IActionResult> CheckUserNameExists([FromBody] ValidateUserNameRequest request) {
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
        public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request) {
            if (!ModelState.IsValid) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var userNameWasProvided = !string.IsNullOrWhiteSpace(request.UserName);
            var availableRules = GetAvailableRules(userNameProvided: !string.IsNullOrWhiteSpace(request.UserName)).ToDictionary(x => x.Key, x => new PasswordRuleInfo {
                Name = x.Key,
                IsValid = true,
                Description = x.Value
            });
            var user = new User { UserName = request.UserName ?? string.Empty, Id = User.FindSubjectId() ?? string.Empty };
            foreach (var validator in _userManager.PasswordValidators) {
                var result = await validator.ValidateAsync(_userManager, user, request.Password);
                if (!result.Succeeded) {
                    foreach (var error in result.Errors) {
                        if (availableRules.ContainsKey(error.Code)) {
                            availableRules[error.Code].IsValid = false;
                        }
                    }
                }
            }
            return Ok(new CredentialsValidationInfo {
                PasswordRules = availableRules.Values.ToList()
            });
        }

        private IDictionary<string, string> GetAvailableRules(bool userNameProvided) {
            var result = new Dictionary<string, string>();
            var passwordOptions = _userManager.Options.Password;
            var errorDescriber = _userManager.ErrorDescriber;
            result.Add(nameof(IdentityErrorDescriber.PasswordTooShort), errorDescriber.PasswordTooShort(passwordOptions.RequiredLength).Description);
            if (passwordOptions.RequiredUniqueChars > 1) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars), errorDescriber.PasswordRequiresUniqueChars(passwordOptions.RequiredUniqueChars).Description);
            }
            if (passwordOptions.RequireNonAlphanumeric) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric), errorDescriber.PasswordRequiresNonAlphanumeric().Description);
            }
            if (passwordOptions.RequireDigit) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresDigit), errorDescriber.PasswordRequiresDigit().Description);
            }
            if (passwordOptions.RequireLowercase) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresLower), errorDescriber.PasswordRequiresLower().Description);
            }
            if (passwordOptions.RequireUppercase) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUpper), errorDescriber.PasswordRequiresUpper().Description);
            }
            var validators = _userManager.PasswordValidators;
            foreach (var validator in validators) {
                var validatorType = validator.GetType();
                validatorType = validatorType.IsGenericType ? validatorType.GetGenericTypeDefinition() : validatorType;
                if (validatorType == typeof(NonCommonPasswordValidator) || validatorType == typeof(NonCommonPasswordValidator<>)) {
                    result.Add(NonCommonPasswordValidator.ErrorDescriber, _messageDescriber.PasswordIsCommon());
                }
                if ((validatorType == typeof(UserNameAsPasswordValidator) || validatorType == typeof(UserNameAsPasswordValidator<>)) && userNameProvided) {
                    result.Add(UserNameAsPasswordValidator.ErrorDescriber, _messageDescriber.PasswordIdenticalToUserName());
                }
                if (validatorType == typeof(PreviousPasswordAwareValidator) || validatorType == typeof(PreviousPasswordAwareValidator<>) || validatorType == typeof(PreviousPasswordAwareValidator<,>) || validatorType == typeof(PreviousPasswordAwareValidator<,,>)) {
                    result.Add(PreviousPasswordAwareValidator.ErrorDescriber, _messageDescriber.PasswordRecentlyUsed());
                }
                if (validatorType == typeof(LatinCharactersPasswordValidator) || validatorType == typeof(LatinCharactersPasswordValidator<>)) {
                    result.Add(LatinCharactersPasswordValidator.ErrorDescriber, _messageDescriber.PasswordIdenticalToUserName());
                }
            }
            return result;
        }
    }
}

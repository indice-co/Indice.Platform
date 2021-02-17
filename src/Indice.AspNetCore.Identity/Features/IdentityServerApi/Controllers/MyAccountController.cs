using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing a user's account.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Scope)]
    [ProblemDetailsExceptionFilter]
    internal class MyAccountController : ControllerBase
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly GeneralSettings _generalSettings;
        private readonly IConfiguration _configuration;
        private readonly IdentityOptions _identityOptions;
        private readonly IdentityServerApiEndpointsOptions _identityServerApiEndpointsOptions;
        private readonly IEmailService _emailService;
        private readonly IEventService _eventService;
        private readonly ISmsServiceFactory _smsServiceFactory;

        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "MyAccount";

        public MyAccountController(
            ExtendedIdentityDbContext<User, Role> dbContext,
            ExtendedUserManager<User> userManager,
            IConfiguration configuration,
            IdentityServerApiEndpointsOptions identityServerApiEndpointsOptions,
            IEmailService emailService,
            IEventService eventService,
            IOptions<GeneralSettings> generalSettings,
            IOptionsSnapshot<IdentityOptions> identityOptions,
            ISmsServiceFactory smsServiceFactory
        ) {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _emailService = emailService;
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
            _identityServerApiEndpointsOptions = identityServerApiEndpointsOptions ?? throw new ArgumentNullException(nameof(identityServerApiEndpointsOptions));
            _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Updates the email of the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the email address.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/email")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateUserEmailRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && await _userManager.IsEmailConfirmedAsync(user)) {
                ModelState.AddModelError(nameof(request.Email).ToLower(), _userManager.MessageDescriber.EmailAlreadyExists(request.Email));
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.SetEmailAsync(user, request.Email);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
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
                       .WithSubject(_userManager.MessageDescriber.UpdateEmailMessageSubject)
                       .WithBody(_userManager.MessageDescriber.UpdateEmailMessageBody(user, token, request.ReturnUrl))
                       .UsingTemplate(_identityServerApiEndpointsOptions.Email.TemplateName)
                       .WithData(data));
            return NoContent();
        }

        /// <summary>
        /// Confirms the email address of a given user.
        /// </summary>
        /// <param name="request"></param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/email/confirmation")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users.Include(x => x.Claims).Where(x => x.Id == userId).SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.EmailConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), _userManager.MessageDescriber.EmailAlreadyConfirmed);
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
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
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/phone-number")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePhoneNumber([FromBody] UpdateUserPhoneNumberRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var currentPhoneNumber = user.PhoneNumber ?? string.Empty;
            if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase) && await _userManager.IsPhoneNumberConfirmedAsync(user)) {
                ModelState.AddModelError(nameof(request.PhoneNumber).ToLower(), _userManager.MessageDescriber.UserAlreadyHasPhoneNumber(request.PhoneNumber));
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            if (!_identityServerApiEndpointsOptions.PhoneNumber.SendOtpOnUpdate) {
                return NoContent();
            }
            var smsService = _smsServiceFactory.Create(request.DeliveryChannel);
            if (smsService == null) {
                throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered.");
            }
            var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber);
            await smsService.SendAsync(request.PhoneNumber, string.Empty, _userManager.MessageDescriber.PhoneNumberVerificationMessage(token));
            return NoContent();
        }

        /// <summary>
        /// Confirms the phone number of the user, using the OTP token.
        /// </summary>
        /// <param name="request">Models the request of a user for phone number confirmation.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/phone-number/confirmation")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmPhoneNumber([FromBody] ConfirmPhoneNumberRequest request) {
            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            var user = await _userManager.Users.Include(x => x.Claims).Where(x => x.Id == userId).SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            if (user.PhoneNumberConfirmed) {
                ModelState.AddModelError(nameof(request.Token).ToLower(), _userManager.MessageDescriber.PhoneNumberAlreadyConfirmed);
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, request.Token);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
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
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/username")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateUserName([FromBody] UpdateUserNameRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.SetUserNameAsync(user, request.UserName);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Changes the password for the current user, but requires the old password to be present.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("my/account/password")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            return NoContent();
        }

        /// <summary>
        /// Generates a password reset token and sends it to the user via email.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPost("my/account/forgot-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request) {
            if (string.IsNullOrEmpty(request.Email)) {
                ModelState.AddModelError("email", "Please provide your email address.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) {
                return NoContent();
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendAsync(user.Email, _userManager.MessageDescriber.ForgotPasswordMessageSubject, _userManager.MessageDescriber.ForgotPasswordMessageBody(user, code));
            return NoContent();
        }

        /// <summary>
        /// Changes the password of the user confirming the code received during forgot password process.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("my/account/forgot-password/confirmation")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        public async Task<IActionResult> ForgotPasswordConfirmation([FromBody] ForgotPasswordVerifyModel request) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) {
                return NoContent();
            }
            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
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
        /// Gets the claims of the user.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("my/account/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<ClaimInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetClaims() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var claims = await _dbContext.UserClaims.Where(x => x.UserId == user.Id).ToListAsync();
            var response = claims.Select(x => new ClaimInfo {
                Id = x.Id,
                Type = x.ClaimType,
                Value = x.ClaimValue
            });
            return Ok(new ResultSet<ClaimInfo>(response, response.Count()));
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
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        /// <response code="410">Gone</response>
        [AllowAnonymous]
        [HttpPost("account/username-exists")]
        [ProducesResponseType(statusCode: StatusCodes.Status302Found, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status410Gone, type: typeof(void))]
        public async Task<IActionResult> CheckUserNameExists([FromBody] ValidateUserNameRequest request) {
            var allowUserEnumeration = _configuration.GetSection(GeneralSettings.Name).GetValue<bool?>("AllowUserEnumeration") ?? _configuration.GetValue<bool?>("AllowUserEnumeration") ?? true;
            if (!allowUserEnumeration) {
                return StatusCode(StatusCodes.Status410Gone);
            }
            if (!ModelState.IsValid) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var user = await _userManager.FindByNameAsync(request.UserName);
            return user == null ? NotFound() : StatusCode(StatusCodes.Status302Found);
        }

        /// <summary>
        /// Validates a user's password against one or more configured <see cref="IPasswordValidator{TUser}"/>.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [AllowAnonymous]
        [HttpPost("account/validate-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CredentialsValidationInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        public async Task<IActionResult> ValidatePassword([FromBody] ValidatePasswordRequest request) {
            if (!ModelState.IsValid) {
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            User user = null;
            if (!string.IsNullOrWhiteSpace(request.Token) && Base64Id.TryParse(request.Token, out var userId)) {
                user = await _userManager.FindByIdAsync(userId.Id.ToString());
            }
            var userAvailable = user != null;
            var userNameAvailable = !string.IsNullOrWhiteSpace(request.UserName);
            var availableRules = GetAvailableRules(userAvailable, userNameAvailable).ToDictionary(rule => rule.Key, rule => new PasswordRuleInfo {
                Code = rule.Key,
                IsValid = true,
                Description = rule.Value.Description,
                Requirement = rule.Value.Hint
            });
            foreach (var validator in _userManager.PasswordValidators) {
                var result = await validator.ValidateAsync(_userManager, user ?? new User(), request.Password ?? string.Empty);
                if (!result.Succeeded) {
                    foreach (var error in result.Errors) {
                        if (availableRules.ContainsKey(error.Code)) {
                            availableRules[error.Code].IsValid = false;
                        }
                    }
                }
            }
            return Ok(new CredentialsValidationInfo { PasswordRules = availableRules.Values.ToList() });
        }

        private IDictionary<string, (string Description, string Hint)> GetAvailableRules(bool userAvailable, bool userNameAvailable) {
            var result = new Dictionary<string, (string Description, string Hint)>();
            var passwordOptions = _userManager.Options.Password;
            var errorDescriber = _userManager.ErrorDescriber as ExtendedIdentityErrorDescriber;
            var messageDescriber = _userManager.MessageDescriber;
            result.Add(nameof(IdentityErrorDescriber.PasswordTooShort), 
                (_userManager.ErrorDescriber.PasswordTooShort(passwordOptions.RequiredLength).Description, Hint: errorDescriber?.PasswordTooShortRequirement(passwordOptions.RequiredLength)));
            if (passwordOptions.RequiredUniqueChars > 1) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars), 
                    (_userManager.ErrorDescriber.PasswordRequiresUniqueChars(passwordOptions.RequiredUniqueChars).Description, Hint: errorDescriber?.PasswordRequiresUniqueCharsRequirement(passwordOptions.RequiredUniqueChars)));
            }
            if (passwordOptions.RequireNonAlphanumeric) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric), 
                    (_userManager.ErrorDescriber.PasswordRequiresNonAlphanumeric().Description, Hint: errorDescriber?.PasswordRequiresNonAlphanumericRequirement));
            }
            if (passwordOptions.RequireDigit) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresDigit), (_userManager.ErrorDescriber.PasswordRequiresDigit().Description, Hint: errorDescriber?.PasswordRequiresDigitRequirement));
            }
            if (passwordOptions.RequireLowercase) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresLower), (_userManager.ErrorDescriber.PasswordRequiresLower().Description, Hint: errorDescriber?.PasswordRequiresLowerRequirement));
            }
            if (passwordOptions.RequireUppercase) {
                result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUpper), (_userManager.ErrorDescriber.PasswordRequiresUpper().Description, Hint: errorDescriber?.PasswordRequiresUpperRequirement));
            }
            var validators = _userManager.PasswordValidators;
            foreach (var validator in validators) {
                var validatorType = validator.GetType();
                validatorType = validatorType.IsGenericType ? validatorType.GetGenericTypeDefinition() : validatorType;
                var isNonCommonPasswordValidator = validatorType == typeof(NonCommonPasswordValidator) || validatorType == typeof(NonCommonPasswordValidator<>);
                if (isNonCommonPasswordValidator) {
                    result.Add(NonCommonPasswordValidator.ErrorDescriber, (Description: messageDescriber.PasswordIsCommon, Hint: messageDescriber.PasswordIsCommonRequirement));
                }
                var isUserNameAsPasswordValidator = validatorType == typeof(UserNameAsPasswordValidator) || validatorType == typeof(UserNameAsPasswordValidator<>);
                if (isUserNameAsPasswordValidator && userNameAvailable) {
                    result.Add(UserNameAsPasswordValidator.ErrorDescriber, (Description: messageDescriber.PasswordIdenticalToUserName, Hint: messageDescriber.PasswordIdenticalToUserNameRequirement));
                }
                var isPreviousPasswordAwareValidator = validatorType == typeof(PreviousPasswordAwareValidator)
                    || validatorType == typeof(PreviousPasswordAwareValidator<>)
                    || validatorType == typeof(PreviousPasswordAwareValidator<,>)
                    || validatorType == typeof(PreviousPasswordAwareValidator<,,>);
                if (isPreviousPasswordAwareValidator && userAvailable) {
                    result.Add(PreviousPasswordAwareValidator.ErrorDescriber, (Description: messageDescriber.PasswordRecentlyUsed, Hint: messageDescriber.PasswordRecentlyUsedRequirement));
                }
                var isLatinCharactersPasswordValidator = validatorType == typeof(LatinLettersOnlyPasswordValidator) || validatorType == typeof(LatinLettersOnlyPasswordValidator<>);
                if (isLatinCharactersPasswordValidator) {
                    result.Add(LatinLettersOnlyPasswordValidator.ErrorDescriber, (Description: messageDescriber.PasswordHasNonLatinChars, Hint: messageDescriber.PasswordHasNonLatinCharsRequirement));
                }
            }
            return result;
        }
    }
}

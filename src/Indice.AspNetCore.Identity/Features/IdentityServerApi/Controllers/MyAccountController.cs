using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using static IdentityServer4.IdentityServerConstants;

namespace Indice.AspNetCore.Identity.Api.Controllers;

/// <summary>Contains operations for managing a user's account.</summary>
/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
/// <response code="500">Internal Server Error</response>
[Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Scope)]
[Route("api")]
[ApiController]
[ApiExplorerSettings(GroupName = "identity")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
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
    private readonly ISmsServiceFactory _smsServiceFactory;
    private readonly ExtendedConfigurationDbContext _configurationDbContext;
    private readonly IPersistedGrantStore _persistedGrantStore;
    private readonly IPersistentGrantSerializer _serializer;

    /// <summary>The name of the controller.</summary>
    public const string Name = "MyAccount";

    public MyAccountController(
        ExtendedIdentityDbContext<User, Role> dbContext,
        ExtendedUserManager<User> userManager,
        IConfiguration configuration,
        IdentityServerApiEndpointsOptions identityServerApiEndpointsOptions,
        IEmailService emailService,
        IOptions<GeneralSettings> generalSettings,
        IOptionsSnapshot<IdentityOptions> identityOptions,
        ISmsServiceFactory smsServiceFactory,
        ExtendedConfigurationDbContext configurationDbContext,
        IPersistedGrantStore persistedGrantStore,
        IPersistentGrantSerializer serializer
    ) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _emailService = emailService;
        _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
        _identityServerApiEndpointsOptions = identityServerApiEndpointsOptions ?? throw new ArgumentNullException(nameof(identityServerApiEndpointsOptions));
        _smsServiceFactory = smsServiceFactory ?? throw new ArgumentNullException(nameof(smsServiceFactory));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        _persistedGrantStore = persistedGrantStore ?? throw new ArgumentNullException(nameof(persistedGrantStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>Updates the email of the current user.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (!_identityServerApiEndpointsOptions.Email.SendEmailOnUpdate) {
            return NoContent();
        }
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _emailService.SendAsync(message => {
            var builder = message
                .To(user.Email)
                .WithSubject(_userManager.MessageDescriber.UpdateEmailMessageSubject);
            if (!string.IsNullOrWhiteSpace(_identityServerApiEndpointsOptions.Email.UpdateEmailTemplate)) {
                var data = new IdentityApiEmailData {
                    DisplayName = User.FindDisplayName() ?? user.UserName,
                    ReturnUrl = request.ReturnUrl,
                    Subject = _userManager.MessageDescriber.UpdateEmailMessageSubject,
                    Token = token,
                    User = user
                };
                builder.UsingTemplate(_identityServerApiEndpointsOptions.Email.UpdateEmailTemplate)
                       .WithData(data);
            } else {
                builder.WithBody(_userManager.MessageDescriber.UpdateEmailMessageBody(user, token, request.ReturnUrl));
            }
        });
        return NoContent();
    }

    /// <summary>Confirms the email address of a given user.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Requests a phone number change for the current user.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (!_identityServerApiEndpointsOptions.PhoneNumber.SendOtpOnUpdate) {
            return NoContent();
        }
        var smsService = _smsServiceFactory.Create(request.DeliveryChannel);
        if (smsService is null) {
            throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered.");
        }
        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber);
        await smsService.SendAsync(request.PhoneNumber, string.Empty, _userManager.MessageDescriber.PhoneNumberVerificationMessage(token));
        return NoContent();
    }

    /// <summary>Confirms the phone number of the user, using the OTP token.</summary>
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
        var user = await _userManager
            .Users
            .Include(x => x.Claims)
            .SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        if (user.PhoneNumberConfirmed) {
            ModelState.AddModelError(nameof(request.Token).ToLower(), _userManager.MessageDescriber.PhoneNumberAlreadyConfirmed);
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, request.Token);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Changes the username for the current user.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return Ok();
    }

    /// <summary>Changes the password for the current user, but requires the old password to be present.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Generates a password reset token and sends it to the user via email.</summary>
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
        var data = new IdentityApiEmailData {
            DisplayName = User.FindDisplayName() ?? user.UserName,
            ReturnUrl = request.ReturnUrl,
            Subject = _userManager.MessageDescriber.ForgotPasswordMessageSubject,
            Token = code,
            User = user
        };
        await _emailService.SendAsync(message => {
            var builder = message
                .To(user.Email)
                .WithSubject(_userManager.MessageDescriber.ForgotPasswordMessageSubject);
            if (!string.IsNullOrWhiteSpace(_identityServerApiEndpointsOptions.Email.ForgotPasswordTemplate)) {
                builder.UsingTemplate(_identityServerApiEndpointsOptions.Email.ForgotPasswordTemplate)
                       .WithData(data);
            } else {
                builder.WithBody(_userManager.MessageDescriber.ForgotPasswordMessageBody(user, code));
            }
        });
        return NoContent();
    }

    /// <summary>Changes the password of the user confirming the code received during forgot password process.</summary>
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
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Updates the password expiration policy.</summary>
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

    /// <summary>Updates the max devices count.</summary>
    /// <param name="request">Models the request to update the max devices number for the user.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [HttpPut("my/account/max-devices-count")]
    [ProducesResponseType(statusCode: StatusCodes.Status202Accepted, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateMaxDevicesCount([FromBody] UpdateMaxDevicesCountRequest request) {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) {
            return NotFound();
        }
        var result = await _userManager.SetMaxDevicesCountAsync(user, request.Count);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return Accepted();
    }

    /// <summary>Gets the claims of the user.</summary>
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

    /// <summary>Gets the consents given by the user.</summary>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("my/account/grants")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserConsentInfo>))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetConsents([FromQuery] ListOptions<UserConsentsListFilter> options) {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) {
            return NotFound();
        }
        var consents = await GetPersistedGrantsAsync(user.Id, options?.Filter?.ClientId, options?.Filter?.ConsentType.ToConstantName());
        return Ok(consents.AsQueryable().ToResultSet(options));
    }

    /// <summary>Adds the requested claims on the current user's account.</summary>
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
        var systemClaims = await _configurationDbContext
            .ClaimTypes
            .Where(x => claims.Select(x => x.Type).Contains(x.Name))
            .ToListAsync();
        var userAllowedClaims = systemClaims.Where(x => x.UserEditable).Select(x => x.Name).ToList();
        var isSystemClient = User.IsSystemClient();
        if (isSystemClient && systemClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(systemClaims.Select(x => x.Name));
            ModelState.AddModelError(nameof(claims), $"The following claims are not allowed to add by the client: '{string.Join(", ", notAllowedClaims)}'.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        if (!isSystemClient && userAllowedClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(userAllowedClaims);
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

    /// <summary>Upserts the requested claims on the current user's account.</summary>
    /// <param name="claims">Contains info about the claims to create.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [HttpPatch("my/account/claims")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<ClaimInfo>))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> PatchClaims([FromBody] IEnumerable<CreateClaimRequest> claims) {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) {
            return NotFound();
        }
        var systemClaims = await _configurationDbContext
            .ClaimTypes
            .Where(x => claims.Select(x => x.Type).Contains(x.Name))
            .ToListAsync();
        var userAllowedClaims = systemClaims.Where(x => x.UserEditable).Select(x => x.Name).ToList();
        var isSystemClient = User.IsSystemClient();
        if (isSystemClient && systemClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(systemClaims.Select(x => x.Name));
            ModelState.AddModelError(nameof(claims), $"The following claims are not allowed to add by the client: '{string.Join(", ", notAllowedClaims)}'.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        if (!isSystemClient && userAllowedClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(userAllowedClaims);
            ModelState.AddModelError(nameof(claims), $"The following claims are not allowed to add: '{string.Join(", ", notAllowedClaims)}'.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var existingUserClaims = await _userManager.GetClaimsAsync(user);
        var claimsToRemove = existingUserClaims.Where(x => systemClaims.Select(x => x.Name).Contains(x.Type));
        if (claimsToRemove.Any()) {
            await _userManager.RemoveClaimsAsync(user, claimsToRemove);
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

    /// <summary>Updates the specified claim for the current user.</summary>
    /// <param name="claimId">The id of the user claim.</param>
    /// <param name="request">Contains info about the claims to update.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad Request</response>
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
        var claimType = await _configurationDbContext.ClaimTypes.SingleOrDefaultAsync(x => x.Name == userClaim.ClaimType);
        if (claimType == null) {
            return NotFound();
        }
        var isSystemClient = User.IsSystemClient();
        var canEditClaim = claimType.UserEditable || isSystemClient;
        if (!canEditClaim) {
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

    /// <summary>Permanently deletes current user's account.</summary>
    /// <response code="204">No Content</response>
    [HttpDelete("my/account")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    public async Task<IActionResult> DeleteAccount() {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _userManager.DeleteAsync(currentUser);
        return NoContent();
    }

    /// <summary>Gets the password options that are applied when the user creates an account.</summary>
    /// <response code="200">OK</response>
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    [HttpGet("account/password-options")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(PasswordOptions))]
    public IActionResult GetPasswordOptions() => Ok(_identityOptions.Password);

    /// <summary>Checks if a username already exists in the database.</summary>
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

    /// <summary>Validates a user's password against one or more configured <see cref="IPasswordValidator{TUser}"/>.</summary>
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
            var userInstance = user ?? (userNameAvailable ? new User { UserName = request.UserName } : new User());
            var result = await validator.ValidateAsync(_userManager, userInstance, request.Password ?? string.Empty);
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

    /// <summary>Self-service user registration endpoint.</summary>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [FeatureGate(IdentityServerApiFeatures.PublicRegistration)]
    [HttpPost("account/register")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    public async Task<IActionResult> Register([FromBody] ApiRegisterRequest request) {
        var user = CreateUserFromRequest(request);
        var requestClaimTypes = request.Claims.Select(x => x.Type);
        var claimTypes = await _configurationDbContext.ClaimTypes.Where(x => requestClaimTypes.Contains(x.Name)).ToListAsync();
        var unknownClaimTypes = requestClaimTypes.Except(claimTypes.Select(x => x.Name));
        if (unknownClaimTypes.Any()) {
            ModelState.AddModelError(string.Empty, $"The following claim types are not supported: '{string.Join(", ", unknownClaimTypes)}'.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var canAddClaims = claimTypes.All(x => x.UserEditable) || User.IsSystemClient();
        if (!canAddClaims) {
            ModelState.AddModelError(nameof(claimTypes), $"The following claims are not editable: '{string.Join(", ", claimTypes.Where(x => !x.UserEditable).Select(x => x.Name))}'.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        foreach (var claim in request.Claims) {
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = claim.Type,
                ClaimValue = claim.Value ?? string.Empty,
                UserId = user.Id
            });
        }
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return NoContent();
    }

    private static User CreateUserFromRequest(ApiRegisterRequest request) {
        var user = new User {
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };
        if (!string.IsNullOrWhiteSpace(request.FirstName)) {
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = JwtClaimTypes.GivenName,
                ClaimValue = request.FirstName ?? string.Empty,
                UserId = user.Id
            });
        }
        if (!string.IsNullOrWhiteSpace(request.LastName)) {
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = JwtClaimTypes.FamilyName,
                ClaimValue = request.LastName ?? string.Empty,
                UserId = user.Id
            });
        }
        user.Claims.Add(new IdentityUserClaim<string> {
            ClaimType = BasicClaimTypes.ConsentCommencial,
            ClaimValue = request.HasAcceptedTerms ? bool.TrueString.ToLower() : bool.FalseString.ToLower(),
            UserId = user.Id
        });
        user.Claims.Add(new IdentityUserClaim<string> {
            ClaimType = BasicClaimTypes.ConsentTerms,
            ClaimValue = request.HasReadPrivacyPolicy ? bool.TrueString.ToLower() : bool.FalseString.ToLower(),
            UserId = user.Id
        });
        user.Claims.Add(new IdentityUserClaim<string> {
            ClaimType = BasicClaimTypes.ConsentTermsDate,
            ClaimValue = $"{DateTime.UtcNow:O}",
            UserId = user.Id
        });
        user.Claims.Add(new IdentityUserClaim<string> {
            ClaimType = BasicClaimTypes.ConsentCommencialDate,
            ClaimValue = $"{DateTime.UtcNow:O}",
            UserId = user.Id
        });
        return user;
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
            var isUnicodeCharactersPasswordValidator = validatorType == typeof(UnicodeCharactersPasswordValidator) || validatorType == typeof(UnicodeCharactersPasswordValidator<>);
            if (isUnicodeCharactersPasswordValidator) {
                result.Add(UnicodeCharactersPasswordValidator.ErrorDescriber, (Description: messageDescriber.PasswordHasNonLatinChars, Hint: messageDescriber.PasswordHasNonLatinCharsRequirement));
            }
            var isNotAllowedCharactersPasswordValidator = validatorType == typeof(AllowedCharactersPasswordValidator) || validatorType == typeof(AllowedCharactersPasswordValidator<>);
            if (isNotAllowedCharactersPasswordValidator) {
                result.Add(AllowedCharactersPasswordValidator.ErrorDescriber, (Description: messageDescriber.PasswordContainsNotAllowedChars, Hint: messageDescriber.PasswordContainsNotAllowedCharsRequirement));
            }
        }
        return result;
    }

    private async Task<IEnumerable<UserConsentInfo>> GetPersistedGrantsAsync(string subjectId, string clientId, string consentType) {
        if (string.IsNullOrWhiteSpace(subjectId)) {
            throw new ArgumentNullException(nameof(subjectId));
        }
        var grants = (await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = consentType
        }))
        .ToArray();
        try {
            var consents = grants
                .Where(x => x.Type == PersistedGrantTypes.UserConsent)
                .Select(x => _serializer.Deserialize<Consent>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.Scopes,
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.Expiration,
                    Type = PersistedGrantTypes.UserConsent
                });
            var codes = grants
                .Where(x => x.Type == PersistedGrantTypes.AuthorizationCode)
                .Select(x => _serializer.Deserialize<AuthorizationCode>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.RequestedScopes,
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.CreationTime.AddSeconds(x.Lifetime),
                    Type = PersistedGrantTypes.AuthorizationCode
                });
            var refresh = grants
                .Where(x => x.Type == PersistedGrantTypes.RefreshToken)
                .Select(x => _serializer.Deserialize<RefreshToken>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.Scopes,
                    Claims = x.AccessToken?.Claims?.Select(x => new BasicClaimInfo {
                        Type = x.Type,
                        Value = x.Value
                    }),
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.CreationTime.AddSeconds(x.Lifetime),
                    Type = PersistedGrantTypes.RefreshToken
                });
            var access = grants
                .Where(x => x.Type == PersistedGrantTypes.ReferenceToken)
                .Select(x => _serializer.Deserialize<Token>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.Scopes,
                    Claims = x.Claims.Select(x => new BasicClaimInfo {
                        Type = x.Type,
                        Value = x.Value
                    }),
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.CreationTime.AddSeconds(x.Lifetime),
                    Type = PersistedGrantTypes.ReferenceToken
                });
            consents = Join(consents, codes);
            consents = Join(consents, refresh);
            consents = Join(consents, access);
            return consents.ToArray();
        } catch (Exception) { }
        return Enumerable.Empty<UserConsentInfo>();
    }

    private static IEnumerable<UserConsentInfo> Join(IEnumerable<UserConsentInfo> first, IEnumerable<UserConsentInfo> second) {
        var list = first.ToList();
        foreach (var other in second) {
            var match = list.FirstOrDefault(x => x.ClientId == other.ClientId);
            if (match != null) {
                match.Claims = match.Claims.Union(other.Claims).Distinct();
                match.Scopes = match.Scopes.Union(other.Scopes).Distinct();
                if (match.CreatedAt > other.CreatedAt) {
                    match.CreatedAt = other.CreatedAt;
                }
                if (match.ExpiresAt == null || other.ExpiresAt == null) {
                    match.ExpiresAt = null;
                } else if (match.ExpiresAt < other.ExpiresAt) {
                    match.ExpiresAt = other.ExpiresAt;
                }
            } else {
                list.Add(other);
            }
        }
        return list;
    }
}

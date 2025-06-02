using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.PasswordValidation;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using static IdentityServer4.IdentityServerConstants;

namespace Indice.Features.Identity.Server.Manager;

internal static partial class MyAccountHandlers
{
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateEmail(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        LinkGenerator linkGenerator,
        HttpContext httpContext,
        ClaimsPrincipal currentUser,
        IEmailService emailService,
        UpdateUserEmailRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var currentEmail = await userManager.GetEmailAsync(user);
        if (currentEmail is not null && currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && await userManager.IsEmailConfirmedAsync(user)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.Email).ToLower(), userManager.MessageDescriber.EmailAlreadyExists(request.Email)));
        }
        var result = await userManager.SetEmailAsync(user, request.Email);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        if (!endpointOptions.Value.Email.SendEmailOnUpdate) {
            return TypedResults.NoContent();
        }
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendAsync(message => {
            var builder = message
                .To(user.Email!)
                .WithSubject(userManager.MessageDescriber.UpdateEmailMessageSubject);
            if (!string.IsNullOrWhiteSpace(endpointOptions.Value.Email.UpdateEmailTemplate)) {
                var data = new EmailChangeEmailModel {
                    DisplayName = currentUser.FindDisplayName() ?? user.UserName,
                    ReturnUrl = request.ReturnUrl,
                    Subject = userManager.MessageDescriber.UpdateEmailMessageSubject,
                    Token = token,
                    Url = linkGenerator.GetUriByPage(httpContext, "/ConfirmEmail", values: new { userId = user.Id, token, email = request.Email, request.ReturnUrl }),
                    User = user,
                    NewEmail = request.Email
                };
                builder.UsingTemplate(endpointOptions.Value.Email.UpdateEmailTemplate)
                       .WithData(data);
            } else {
                builder.WithBody(userManager.MessageDescriber.UpdateEmailMessageBody(user, token, request.ReturnUrl));
            }
        });
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ConfirmEmail(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        ConfirmEmailRequest request
    ) {
        var userId = currentUser.FindFirstValue(JwtClaimTypes.Subject);
        var user = await userManager.Users
                                    .Include(x => x.Claims)
                                    .Where(x => x.Id == userId)
                                    .SingleOrDefaultAsync();
        if (user == null) {
            return TypedResults.NotFound();
        }
        if (user.EmailConfirmed) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(request.Token).ToLower(), userManager.MessageDescriber.EmailAlreadyConfirmed)
            );
        }
        var result = await userManager.ConfirmEmailAsync(user, request.Token!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> EmailChange(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        LinkGenerator linkGenerator,
        HttpContext httpContext,
        ClaimsPrincipal currentUser,
        IEmailService emailService,
        ChangeUserEmailRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var currentEmail = await userManager.GetEmailAsync(user);
        if (currentEmail is not null && currentEmail.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && await userManager.IsEmailConfirmedAsync(user)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.Email).ToLower(), userManager.MessageDescriber.EmailAlreadyExists(request.Email)));
        }
        var token = await userManager.GenerateChangeEmailTokenAsync(user, request.Email);
        await emailService.SendAsync(message => {
            var builder = message
                .To(request.Email)
                .WithSubject(userManager.MessageDescriber.ConfirmationEmailChangeSubject);
            if (!string.IsNullOrWhiteSpace(endpointOptions.Value.Email.UpdateEmailTemplate)) {
                var data = new EmailChangeEmailModel {
                    DisplayName = currentUser.FindDisplayName() ?? user.UserName,
                    ReturnUrl = request.ReturnUrl,
                    Subject = userManager.MessageDescriber.ConfirmationEmailChangeSubject,
                    Token = token,
                    Url = linkGenerator.GetUriByPage(httpContext, "/ConfirmEmailChange", values: new { userId = user.Id, token, email = request.Email, request.ReturnUrl }),
                    User = user,
                    NewEmail = request.Email
                };
                builder.UsingTemplate(endpointOptions.Value.Email.ChangeEmailTemplate)
                       .WithData(data);
            } else {
                builder.WithBody(userManager.MessageDescriber.ChangeEmailMessageBody(user, token, request.Email, request.ReturnUrl));
            }
        });
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ConfirmEmailChange(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        ConfirmEmailChangeRequest request
    ) {
        var userId = currentUser.FindFirstValue(JwtClaimTypes.Subject);
        var user = await userManager.Users
                                    .Include(x => x.Claims)
                                    .Where(x => x.Id == userId)
                                    .SingleOrDefaultAsync();
        if (user == null) {
            return TypedResults.NotFound();
        }
        if (user.Email == request.Email && user.EmailConfirmed) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(request.Token).ToLower(), userManager.MessageDescriber.EmailAlreadyConfirmed)
            );
        }
        var result = await userManager.ChangeEmailAsync(user, request.Email!, request.Token!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePhoneNumber(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        ISmsServiceFactory smsServiceFactory,
        UpdateUserPhoneNumberRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var currentPhoneNumber = user.PhoneNumber ?? string.Empty;
        if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase) && await userManager.IsPhoneNumberConfirmedAsync(user)) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(request.PhoneNumber).ToLower(), userManager.MessageDescriber.UserAlreadyHasPhoneNumber(request.PhoneNumber))
            );
        }
        var result = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        if (!endpointOptions.Value.PhoneNumber.SendOtpOnUpdate) {
            return TypedResults.NoContent();
        }
        var smsService = smsServiceFactory.Create(request.DeliveryChannel!) ?? throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered.");

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber!);
        await smsService.SendAsync(request.PhoneNumber!, string.Empty, userManager.MessageDescriber.PhoneNumberVerificationMessage(token));
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> PhoneNumberChange(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        ClaimsPrincipal currentUser,
        ISmsServiceFactory smsServiceFactory,
        ChangeUserPhoneNumberRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var currentPhoneNumber = user.PhoneNumber ?? string.Empty;
        if (currentPhoneNumber.Equals(request.PhoneNumber, StringComparison.OrdinalIgnoreCase) && await userManager.IsPhoneNumberConfirmedAsync(user)) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(request.PhoneNumber).ToLower(), userManager.MessageDescriber.UserAlreadyHasPhoneNumber(request.PhoneNumber))
            );
        }
        var smsService = smsServiceFactory.Create(request.DeliveryChannel!) ?? throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered.");
        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber!);
        await smsService.SendAsync(request.PhoneNumber!, string.Empty, userManager.MessageDescriber.PhoneNumberChangeVerificationMessage(token));
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ConfirmPhoneNumber(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        ConfirmPhoneNumberRequest request
    ) {
        var userId = currentUser.FindFirstValue(JwtClaimTypes.Subject);
        var user = await userManager
            .Users
            .Include(x => x.Claims)
            .SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return TypedResults.NotFound();
        }
        if (user.PhoneNumberConfirmed) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(request.Token).ToLower(), userManager.MessageDescriber.PhoneNumberAlreadyConfirmed)
            );
        }
        var result = await userManager.ChangePhoneNumberAsync(user, user.PhoneNumber!, request.Token!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ConfirmPhoneNumberChange(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        ConfirmPhoneNumberChangeRequest request
    ) {
        var userId = currentUser.FindFirstValue(JwtClaimTypes.Subject);
        var user = await userManager
            .Users
            .Include(x => x.Claims)
            .SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.ChangePhoneNumberAsync(user, request.PhoneNumber!, request.Token!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> BlockAccount(
        ExtendedUserManager<User> userManager,
        IFeatureManager featureManager,
        ClaimsPrincipal currentUser,
        SetUserBlockRequest request
    ) {
        if (!await featureManager.IsEnabledAsync(IdentityEndpoints.Features.PublicRegistration)) {
            return TypedResults.NotFound();
        }
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.SetBlockedAsync(user, request.Blocked);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateUserName(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        UpdateUserNameRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.SetUserNameAsync(user, request.UserName);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePassword(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        ChangePasswordRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.ChangePasswordAsync(user, request.OldPassword!, request.NewPassword!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> ForgotPassword(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        IEmailService emailService,
        ForgotPasswordRequest request
    ) {
        if (string.IsNullOrEmpty(request.Email)) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError("email", "Please provide your email address.")
            );
        }
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null) {
            return TypedResults.NoContent();
        }
        var code = await userManager.GeneratePasswordResetTokenAsync(user);
        var data = new EmailChangeEmailModel {
            DisplayName = currentUser.FindDisplayName() ?? user.UserName,
            ReturnUrl = request.ReturnUrl,
            Subject = userManager.MessageDescriber.ForgotPasswordMessageSubject,
            Token = code,
            User = user
        };
        await emailService.SendAsync(message => {
            var builder = message
                .To(user.Email!)
                .WithSubject(userManager.MessageDescriber.ForgotPasswordMessageSubject);
            if (!string.IsNullOrWhiteSpace(endpointOptions.Value.Email.ForgotPasswordTemplate)) {
                builder.UsingTemplate(endpointOptions.Value.Email.ForgotPasswordTemplate)
                       .WithData(data);
            } else {
                builder.WithBody(userManager.MessageDescriber.ForgotPasswordMessageBody(user, code));
            }
        });
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, ValidationProblem>> ForgotPasswordConfirmation(
        ExtendedUserManager<User> userManager,
        ForgotPasswordConfirmationRequest request
    ) {
        var user = await userManager.FindByEmailAsync(request.Email!);
        if (user == null) {
            return TypedResults.NoContent();
        }
        var result = await userManager.ResetPasswordAsync(user, request.Token!, request.NewPassword!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> UpdatePasswordExpirationPolicy(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        UpdatePasswordExpirationPolicyRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        await userManager.SetPasswordExpirationPolicyAsync(user, request.Policy);
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateMaxDevicesCount(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        UpdateMaxDevicesCountRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.SetMaxDevicesCountAsync(user, request.Count);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<Ok<ResultSet<ClaimInfo>>, NotFound, ValidationProblem>> GetClaims(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        ClaimsPrincipal currentUser
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var claims = await dbContext.UserClaims.Where(x => x.UserId == user.Id).ToListAsync();
        var response = claims.Select(x => new ClaimInfo {
            Id = x.Id,
            Type = x.ClaimType,
            Value = x.ClaimValue
        });
        return TypedResults.Ok(response.ToResultSet());
    }

    internal static async Task<Results<Ok<ResultSet<ClaimInfo>>, NotFound, ValidationProblem>> AddClaims(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        ExtendedConfigurationDbContext configurationDbContext,
        ClaimsPrincipal currentUser,
        List<CreateClaimRequest> claims
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var systemClaims = await configurationDbContext
            .ClaimTypes
            .Where(x => claims.Select(x => x.Type).Contains(x.Name))
            .ToListAsync();
        var userAllowedClaims = systemClaims.Where(x => x.UserEditable).Select(x => x.Name).ToList();
        var isSystemClient = currentUser.IsSystemClient();
        if (isSystemClient && systemClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(systemClaims.Select(x => x.Name));
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claims), $"The following claims are not allowed to add by the client: '{string.Join(", ", notAllowedClaims)}'.")
            );
        }
        if (!isSystemClient && userAllowedClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(userAllowedClaims);
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claims), $"The following claims are not allowed to add: '{string.Join(", ", notAllowedClaims)}'.")
            );
        }
        var claimsToAdd = claims.Select(x => new IdentityUserClaim<string> {
            UserId = user.Id,
            ClaimType = x.Type,
            ClaimValue = x.Value
        }).ToArray();
        dbContext.UserClaims.AddRange(claimsToAdd);
        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(claimsToAdd.Select(x => new ClaimInfo {
            Id = x.Id,
            Type = x.ClaimType,
            Value = x.ClaimValue
        }).ToResultSet());
    }

    internal static async Task<Results<Ok<ResultSet<ClaimInfo>>, NotFound, ValidationProblem>> PatchClaims(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        ExtendedConfigurationDbContext configurationDbContext,
        ClaimsPrincipal currentUser,
        List<CreateClaimRequest> claims
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var systemClaims = await configurationDbContext
            .ClaimTypes
            .Where(x => claims.Select(x => x.Type).Contains(x.Name))
            .ToListAsync();
        var userAllowedClaims = systemClaims.Where(x => x.UserEditable).Select(x => x.Name).ToList();
        var isSystemClient = currentUser.IsSystemClient();
        if (isSystemClient && systemClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(systemClaims.Select(x => x.Name));
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claims), $"The following claims are not allowed to add by the client: '{string.Join(", ", notAllowedClaims)}'.")
            );
        }
        if (!isSystemClient && userAllowedClaims.Count != claims.Count()) {
            var notAllowedClaims = claims.Select(x => x.Type).Except(userAllowedClaims);
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claims), $"The following claims are not allowed to add: '{string.Join(", ", notAllowedClaims)}'.")
            );
        }
        var existingUserClaims = await userManager.GetClaimsAsync(user);
        var claimsToRemove = existingUserClaims.Where(x => systemClaims.Select(x => x.Name).Contains(x.Type));
        if (claimsToRemove.Any()) {
            await userManager.RemoveClaimsAsync(user, claimsToRemove);
        }
        var claimsToAdd = claims.Select(x => new IdentityUserClaim<string> {
            UserId = user.Id,
            ClaimType = x.Type,
            ClaimValue = x.Value
        })
        .ToArray();
        dbContext.UserClaims.AddRange(claimsToAdd);
        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(claimsToAdd.Select(x => new ClaimInfo {
            Id = x.Id,
            Type = x.ClaimType,
            Value = x.ClaimValue
        }).ToResultSet());
    }

    internal static async Task<Results<Ok<ClaimInfo>, NotFound, ValidationProblem>> UpdateClaim(
        ExtendedUserManager<User> userManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        ExtendedConfigurationDbContext configurationDbContext,
        ClaimsPrincipal currentUser,
        int claimId, UpdateUserClaimRequest request
    ) {
        var userId = currentUser.FindSubjectId();
        var userClaim = await dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
        if (userClaim == null) {
            return TypedResults.NotFound();
        }
        var claimType = await configurationDbContext.ClaimTypes.SingleOrDefaultAsync(x => x.Name == userClaim.ClaimType);
        if (claimType == null) {
            return TypedResults.NotFound();
        }
        var isSystemClient = currentUser.IsSystemClient();
        var canEditClaim = claimType.UserEditable || isSystemClient;
        if (!canEditClaim) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claimType), $"Claim '{claimType.Name}' is not editable.")
            );
        }
        userClaim.ClaimValue = request.ClaimValue;
        await dbContext.SaveChangesAsync();
        return TypedResults.Ok(new ClaimInfo {
            Id = userClaim.Id,
            Type = userClaim.ClaimType,
            Value = request.ClaimValue
        });
    }

    internal static async Task<Results<Ok<ResultSet<UserClientInfo>>, NotFound>> GetConsents(
        ExtendedUserManager<User> userManager,
        IPersistedGrantStore grants,
        IPersistentGrantSerializer serializer,
        ClaimsPrincipal currentUser,
        [AsParameters] ListOptions options,
        [AsParameters] UserConsentsListFilter filter
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var consents = await grants.GetAllGroupedByClientAsync(serializer, user.Id, filter?.ClientId, filter?.ConsentType.ToConstantName());
        return TypedResults.Ok(consents.AsQueryable().ToResultSet(options));
    }

    internal static async Task<Results<NoContent, NotFound>> RevokeConsents(
        ExtendedUserManager<User> userManager, 
        IPersistedGrantService grants,
        IEventService events,
        ClaimsPrincipal currentUser,
        string clientId) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        await grants.RemoveAllGrantsAsync(currentUser.GetSubjectId(), clientId);
        await events.RaiseAsync(new GrantsRevokedEvent(currentUser.GetSubjectId(), clientId));
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> RevokeAllConsents(
        ExtendedUserManager<User> userManager,
        IPersistedGrantService grants,
        IEventService events,
        ClaimsPrincipal currentUser) {

        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        await grants.RemoveAllGrantsAsync(currentUser.GetSubjectId());
        await events.RaiseAsync(new GrantsRevokedEvent(currentUser.GetSubjectId(), null));
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteAccount(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static Results<Ok<PasswordOptions>, NotFound> GetPasswordOptions(IOptionsSnapshot<IdentityOptions> identityOptions) {
        if (identityOptions.Value is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(identityOptions.Value.Password);
    }

    internal static async Task<Ok<CredentialsValidationInfo>> ValidatePassword(
        ExtendedUserManager<User> userManager,
        ValidatePasswordRequest request
    ) {
        User? user = null;
        if (!string.IsNullOrWhiteSpace(request.Token) && Base64Id.TryParse(request.Token, out var userId)) {
            user = await userManager.FindByIdAsync(userId.Id.ToString());
        }
        var userAvailable = user != null;
        var userNameAvailable = !string.IsNullOrWhiteSpace(request.UserName);
        var availableRules = userManager.GetAvailableRules(userAvailable, userNameAvailable).ToDictionary(rule => rule.Key, rule => new PasswordRuleInfo {
            Code = rule.Key,
            IsValid = true,
            Description = rule.Value.Description,
            Requirement = rule.Value.Hint
        });
        foreach (var validator in userManager.PasswordValidators) {
            var userInstance = user ?? (userNameAvailable ? new User { UserName = request.UserName } : new User());
            var result = await validator.ValidateAsync(userManager, userInstance, request.Password ?? string.Empty);
            if (!result.Succeeded) {
                foreach (var error in result.Errors) {
                    if (availableRules.TryGetValue(error.Code, out var value)) {
                        value.IsValid = false;
                    }
                }
            }
        }
        return TypedResults.Ok(new CredentialsValidationInfo {
            PasswordRules = availableRules.Values.ToList()
        });
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> Register(
        ExtendedUserManager<User> userManager,
        ExtendedConfigurationDbContext configurationDbContext,
        ClaimsPrincipal currentUser,
        IFeatureManager featureManager,
        RegisterRequest request
    ) {
        if (!await featureManager.IsEnabledAsync(IdentityEndpoints.Features.PublicRegistration)) {
            return TypedResults.NotFound();
        }
        var user = CreateUserFromRequest(request);
        var requestClaimTypes = request.Claims.Select(x => x.Type);
        var claimTypes = await configurationDbContext.ClaimTypes.Where(x => requestClaimTypes.Contains(x.Name)).ToListAsync();
        var unknownClaimTypes = requestClaimTypes.Except(claimTypes.Select(x => x.Name));
        if (unknownClaimTypes.Any()) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(string.Empty, $"The following claim types are not supported: '{string.Join(", ", unknownClaimTypes)}'.")
            );
        }
        var canAddClaims = claimTypes.All(x => x.UserEditable) || currentUser.IsSystemClient();
        if (!canAddClaims) {
            return TypedResults.ValidationProblem(
                ValidationErrors.AddError(nameof(claimTypes), $"The following claims are not editable: '{string.Join(", ", claimTypes.Where(x => !x.UserEditable).Select(x => x.Name))}'.")
            );
        }
        foreach (var claim in request.Claims) {
            user.Claims.Add(new IdentityUserClaim<string> {
                ClaimType = claim.Type,
                ClaimValue = claim.Value ?? string.Empty,
                UserId = user.Id
            });
        }
        var result = await userManager.CreateAsync(user, request.Password!);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        //var token = await userManager.GenerateEmailConfirmationTokenAsync(user); // in case we need this
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, StatusCodeHttpResult, NotFound, ValidationProblem>> CheckUserNameExists(
        ExtendedUserManager<User> userManager,
        IConfiguration configuration,
        ValidateUserNameRequest request
    ) {
        var allowUserEnumeration = configuration.GetValue<bool?>("General:AllowUserEnumeration") ??
                                   configuration.GetValue<bool?>("AllowUserEnumeration") ?? true;
        if (!allowUserEnumeration) {
            return TypedResults.StatusCode(StatusCodes.Status410Gone);
        }
        var user = await userManager.FindByNameAsync(request.UserName!);
        return user == null ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    internal static Ok<List<CallingCode>> GetSupportedCallingCodes(CallingCodesProvider callingCodesProvider) {
        return TypedResults.Ok(callingCodesProvider.GetSupportedCallingCodes());
    }

    private static User CreateUserFromRequest(RegisterRequest request) {
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
            ClaimType = BasicClaimTypes.ConsentCommercial,
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
            ClaimType = BasicClaimTypes.ConsentCommercialDate,
            ClaimValue = $"{DateTime.UtcNow:O}",
            UserId = user.Id
        });
        return user;
    }

    private static IDictionary<string, (string Description, string? Hint)> GetAvailableRules(this ExtendedUserManager<User> userManager, bool userAvailable, bool userNameAvailable) {
        var result = new Dictionary<string, (string Description, string? Hint)>();
        var passwordOptions = userManager.Options.Password;
        var errorDescriber = userManager.ErrorDescriber as ExtendedIdentityErrorDescriber;
        var messageDescriber = userManager.MessageDescriber;
        result.Add(nameof(IdentityErrorDescriber.PasswordTooShort),
            (userManager.ErrorDescriber.PasswordTooShort(passwordOptions.RequiredLength).Description, Hint: errorDescriber?.PasswordTooShortRequirement(passwordOptions.RequiredLength)));
        if (passwordOptions.RequiredUniqueChars > 1) {
            result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
                (userManager.ErrorDescriber.PasswordRequiresUniqueChars(passwordOptions.RequiredUniqueChars).Description, Hint: errorDescriber?.PasswordRequiresUniqueCharsRequirement(passwordOptions.RequiredUniqueChars)));
        }
        if (passwordOptions.RequireNonAlphanumeric) {
            result.Add(nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
                (userManager.ErrorDescriber.PasswordRequiresNonAlphanumeric().Description, Hint: errorDescriber?.PasswordRequiresNonAlphanumericRequirement));
        }
        if (passwordOptions.RequireDigit) {
            result.Add(nameof(IdentityErrorDescriber.PasswordRequiresDigit), (userManager.ErrorDescriber.PasswordRequiresDigit().Description, Hint: errorDescriber?.PasswordRequiresDigitRequirement));
        }
        if (passwordOptions.RequireLowercase) {
            result.Add(nameof(IdentityErrorDescriber.PasswordRequiresLower), (userManager.ErrorDescriber.PasswordRequiresLower().Description, Hint: errorDescriber?.PasswordRequiresLowerRequirement));
        }
        if (passwordOptions.RequireUppercase) {
            result.Add(nameof(IdentityErrorDescriber.PasswordRequiresUpper), (userManager.ErrorDescriber.PasswordRequiresUpper().Description, Hint: errorDescriber?.PasswordRequiresUpperRequirement));
        }
        var validators = userManager.PasswordValidators;
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

    /// <summary>
    /// Get all persisted grants for a user grouped by client id.
    /// </summary>
    /// <param name="persistedGrantStore">The grant store to extend</param>
    /// <param name="serializer">The persisted grant serializer to use for inspecting the grant data</param>
    /// <param name="subjectId">The user id</param>
    /// <param name="clientId">The client id</param>
    /// <param name="grantType">The grant type</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static async Task<IEnumerable<UserClientInfo>> GetAllGroupedByClientAsync(
        this IPersistedGrantStore persistedGrantStore,
        IPersistentGrantSerializer serializer,
        string subjectId,
        string? clientId = null,
        string? grantType = null
    ) {
        if (string.IsNullOrWhiteSpace(subjectId)) {
            throw new ArgumentNullException(nameof(subjectId));
        }
        var grants = (await persistedGrantStore.GetAllAsync(new PersistedGrantFilter {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = grantType
        }))
        .ToArray();
        try {
            var consents = grants.OrderBy(x => x.CreationTime)
                                  .GroupBy(x => x.ClientId)
                                  .Select(group => {
                                      var info = new UserClientInfo {
                                          ClientId = group.Key,
                                      };
                                      foreach (var grant in group) {
                                          switch (grant.Type) {
                                              case PersistedGrantTypes.UserConsent:
                                                  var consent = serializer.Deserialize<Consent>(grant.Data);
                                                  info.UpdateWith(PersistedGrantTypes.UserConsent, consent.CreationTime, consent.Expiration, consent.Scopes);
                                                  info.Grants.Add(new UserGrantInfo {
                                                      Type = PersistedGrantTypes.UserConsent,
                                                      SessionId = grant.SessionId,
                                                      CreatedAt = consent.CreationTime,
                                                      ExpiresAt = consent.Expiration,
                                                  });
                                                  break;
                                              case PersistedGrantTypes.AuthorizationCode:
                                                  var code = serializer.Deserialize<AuthorizationCode>(grant.Data);
                                                  info.UpdateWith(PersistedGrantTypes.AuthorizationCode, code.CreationTime, code.CreationTime.AddSeconds(code.Lifetime), code.RequestedScopes);
                                                  info.Grants.Add(new UserGrantInfo {
                                                      Type = PersistedGrantTypes.AuthorizationCode,
                                                      SessionId = grant.SessionId,
                                                      CreatedAt = code.CreationTime,
                                                      ExpiresAt = code.CreationTime.AddSeconds(code.Lifetime),
                                                  });
                                                  break;
                                              case PersistedGrantTypes.RefreshToken:
                                                  var refresh = serializer.Deserialize<RefreshToken>(grant.Data);
                                                  info.UpdateWith(PersistedGrantTypes.RefreshToken, refresh.CreationTime, refresh.CreationTime.AddSeconds(refresh.Lifetime), refresh.Scopes);
                                                  info.Grants.Add(new UserGrantInfo {
                                                      Type = PersistedGrantTypes.RefreshToken,
                                                      SessionId = grant.SessionId,
                                                      CreatedAt = refresh.CreationTime,
                                                      ExpiresAt = refresh.CreationTime.AddSeconds(refresh.Lifetime),
                                                      TokenId = refresh.AccessToken?.Claims?.FirstOrDefault(x => x.Type == JwtClaimTypes.JwtId)?.Value,
                                                      DeviceId = refresh.AccessToken?.Claims?.FirstOrDefault(x => x.Type == BasicClaimTypes.DeviceId)?.Value,
                                                      IpAddress = refresh.AccessToken?.Claims?.FirstOrDefault(x => x.Type == BasicClaimTypes.IPAddress)?.Value,
                                                  });
                                                  break;
                                              case PersistedGrantTypes.ReferenceToken:
                                                  var token = serializer.Deserialize<Token>(grant.Data);
                                                  info.UpdateWith(PersistedGrantTypes.ReferenceToken, token.CreationTime, token.CreationTime.AddSeconds(token.Lifetime), token.Scopes);
                                                  info.Grants.Add(new UserGrantInfo {
                                                      Type = PersistedGrantTypes.ReferenceToken,
                                                      SessionId = grant.SessionId,
                                                      CreatedAt = token.CreationTime,
                                                      ExpiresAt = token.CreationTime.AddSeconds(token.Lifetime),
                                                      TokenId = token.Claims?.FirstOrDefault(x => x.Type == JwtClaimTypes.JwtId)?.Value,
                                                      DeviceId = token.Claims?.FirstOrDefault(x => x.Type == BasicClaimTypes.DeviceId)?.Value,
                                                      IpAddress = token.Claims?.FirstOrDefault(x => x.Type == BasicClaimTypes.IPAddress)?.Value,
                                                  });
                                                  break;
                                              default:
                                                  break;
                                          }
                                      }
                                      return info;
                                  }).ToList();
            
            return consents;
        } catch (Exception) { }
        return [];
    }

    private static readonly HashSet<string> _grantClaimTypesToInclude = [
        JwtClaimTypes.JwtId,
        //JwtClaimTypes.SessionId,
        //JwtClaimTypes.IssuedAt,
        //JwtClaimTypes.AuthenticationMethod,
        BasicClaimTypes.IPAddress,
        BasicClaimTypes.DeviceId,
        ];
    private static Func<BasicClaimInfo, bool> OnlyRelevantGrantClaims => x => _grantClaimTypesToInclude.Contains(x.Type!);
}

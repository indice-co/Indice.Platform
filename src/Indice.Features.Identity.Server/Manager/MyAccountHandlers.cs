using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
using Indice.Extensions;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using static IdentityServer4.IdentityServerConstants;

namespace Indice.Features.Identity.Server.Manager;

internal static class MyAccountHandlers
{
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateEmail(
        ExtendedUserManager<User> userManager,
        IOptions<ExtendedEndpointOptions> endpointOptions,
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
                .To(user.Email)
                .WithSubject(userManager.MessageDescriber.UpdateEmailMessageSubject);
            if (!string.IsNullOrWhiteSpace(endpointOptions.Value.Email.UpdateEmailTemplate)) {
                var data = new IdentityApiEmailData {
                    DisplayName = currentUser.FindDisplayName() ?? user.UserName,
                    ReturnUrl = request.ReturnUrl,
                    Subject = userManager.MessageDescriber.UpdateEmailMessageSubject,
                    Token = token,
                    User = user
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
        var smsService = smsServiceFactory.Create(request.DeliveryChannel) ?? throw new Exception($"No concrete implementation of {nameof(ISmsService)} is registered.");

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user, request.PhoneNumber!);
        await smsService.SendAsync(request.PhoneNumber, string.Empty, userManager.MessageDescriber.PhoneNumberVerificationMessage(token));
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
        var data = new IdentityApiEmailData {
            DisplayName = currentUser.FindDisplayName() ?? user.UserName,
            ReturnUrl = request.ReturnUrl,
            Subject = userManager.MessageDescriber.ForgotPasswordMessageSubject,
            Token = code,
            User = user
        };
        await emailService.SendAsync(message => {
            var builder = message
                .To(user.Email)
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

    internal static async Task<Results<Ok<ResultSet<UserConsentInfo>>, NotFound>> GetConsents(
        ExtendedUserManager<User> userManager,
        IPersistedGrantStore persistedGrantStore,
        IPersistentGrantSerializer serializer,
        ClaimsPrincipal currentUser,
        [AsParameters] ListOptions options,
        [AsParameters] UserConsentsListFilter filter
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        var consents = await persistedGrantStore.GetPersistedGrantsAsync(serializer, user.Id, filter?.ClientId, filter?.ConsentType.ToConstantName());
        return TypedResults.Ok(consents.AsQueryable().ToResultSet(options));
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

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateMyAvatar(FileUploadRequest request,
        ExtendedUserManager<User> userManager,
        LinkGenerator linkGenerator,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        IOutputCacheStore cache,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken
        ) {
        if (endpointOptions.Value.AvatarOptions.IsAvatarEnabled) {
            return TypedResults.NotFound();
        }
        var user = await userManager.GetUserAsync(currentUser);
        if (user == null) {
            return TypedResults.NotFound();
        }
        if (!(request.File?.Length > 0)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.File), "The file is mandatory"));
        }
        var avatarDataClaimType = JwtClaimTypes.Picture + "_data";
        // manipulate image resize to max side size.
        using var stream = request.File?.OpenReadStream();
        using var image = Image.Load(stream, out IImageFormat format);
        var maxDimention = endpointOptions.Value.AvatarOptions.AllowedSizes.Max();
        image.Mutate(i => i.Resize(new Size(maxDimention, maxDimention)));

        var imageStream = new MemoryStream();
        await image.SaveAsJpegAsync(imageStream);
        imageStream.Seek(0, SeekOrigin.Begin);
        var data = new StringBuilder();
        data.AppendFormat("data:image/{0};base64,", "jpeg");
        data.Append(Convert.ToBase64String(imageStream.ToArray()));

        var result = await userManager.ReplaceClaimAsync(user, avatarDataClaimType, data.ToString());

        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        var route = linkGenerator.GetUriByName(httpContext, nameof(MyAccountHandlers.GetAvatar), new { userId = user.Id });
        // concatenate with relative absolute Authority.
        //$"{endpointOptions.Value.ApiPrefix}/users/{currentUser.FindSubjectId}/avatar"
        /*
        * The update my avatar should invalidate the cache.
        * The update my avatar resize the bounding box with max side size to sqare. 
        */
        var evictionKey = $"GetAvatar-userId:{user.Id}";
        await cache.EvictByTagAsync(evictionKey, cancellationToken);
        return TypedResults.NoContent();
    }

    private static string GetImageContent(string fileExtention) {
        var dicSupportedFormats = new Dictionary<string, string> {
            [".jpg"] = "jpeg",
            [".jpeg"] = "jpeg",
            [".png"] = "png",
            [".gif"] = "gif"
        };

        if (dicSupportedFormats.TryGetValue(fileExtention, out var fileType))
            return fileType;

        return "jpeg";
    }
    /// <summary>
    /// Retrieve user avatar 
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="endpointOptions"></param>
    /// <param name="userId"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    internal static async Task<Results<FileContentHttpResult, NotFound, ValidationProblem>> GetAvatar(
        ExtendedIdentityDbContext<User, Role> dbContext,
        IOptions<ExtendedEndpointOptions> endpointOptions,
        string userId,
        [FromQuery] int size = 0) {
        if (endpointOptions.Value.AvatarOptions.IsAvatarEnabled)
            return TypedResults.ValidationProblem(ValidationErrors.AddError("", "Avatar feature is not enabled"));

        if (size > 0 && !endpointOptions.Value.AvatarOptions.AllowedSizes.Contains(size)) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError("size", $"The specified size is not in the allowed list ({string.Join(',', endpointOptions.Value.AvatarOptions.AllowedSizes)})"));
        }
        //var user = await dbContext.Users.AsNoTracking()
        //                .Include(x => x.Claims)
        //                .FirstOrDefaultAsync(x => x.Id == userId);

        //if (user == null) {
        //    return TypedResults.NotFound();
        //}

        var avatarDataClaimType = JwtClaimTypes.Picture + "_data";
        var avatarBinary = await dbContext.UserClaims.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.ClaimType == avatarDataClaimType);
        if (avatarBinary != null && !string.IsNullOrEmpty(avatarBinary.ClaimValue)) {
            var mime = Regex.Match(avatarBinary.ClaimValue, "(?<=data[:])(.*)(?=[;])")!.Value;
            var base64 = Regex.Match(avatarBinary.ClaimValue, "(?<=[;]base64[,])(.*)")!.Value;
            if (size == 0 || size == endpointOptions.Value.AvatarOptions.AllowedSizes.Max())
                return TypedResults.File(Convert.FromBase64String(base64), contentType: mime);

            var stream = new MemoryStream(Convert.FromBase64String(base64));
            using var image = Image.Load(stream, out IImageFormat format);
            image.Mutate(i => i.Resize(new Size(size, size)));

            var imageStream = new MemoryStream();
            await image.SaveAsJpegAsync(imageStream);
            imageStream.Seek(0, SeekOrigin.Begin);

            return TypedResults.File(imageStream.ToArray(), contentType: mime);
        }

        return TypedResults.NotFound();
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

    private static async Task<IEnumerable<UserConsentInfo>> GetPersistedGrantsAsync(
        this IPersistedGrantStore persistedGrantStore,
        IPersistentGrantSerializer serializer,
        string subjectId,
        string? clientId,
        string? consentType
    ) {
        if (string.IsNullOrWhiteSpace(subjectId)) {
            throw new ArgumentNullException(nameof(subjectId));
        }
        var grants = (await persistedGrantStore.GetAllAsync(new PersistedGrantFilter {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = consentType
        }))
        .ToArray();
        try {
            var consents = grants
                .Where(x => x.Type == PersistedGrantTypes.UserConsent)
                .Select(x => serializer.Deserialize<Consent>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.Scopes,
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.Expiration,
                    Type = PersistedGrantTypes.UserConsent
                });
            var codes = grants
                .Where(x => x.Type == PersistedGrantTypes.AuthorizationCode)
                .Select(x => serializer.Deserialize<AuthorizationCode>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.RequestedScopes,
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.CreationTime.AddSeconds(x.Lifetime),
                    Type = PersistedGrantTypes.AuthorizationCode
                });
            var refresh = grants
                .Where(x => x.Type == PersistedGrantTypes.RefreshToken)
                .Select(x => serializer.Deserialize<RefreshToken>(x.Data))
                .Select(x => new UserConsentInfo {
                    ClientId = x.ClientId,
                    Scopes = x.Scopes,
                    Claims = x.AccessToken?.Claims?.Select(x => new BasicClaimInfo {
                        Type = x.Type,
                        Value = x.Value
                    }) ?? new List<BasicClaimInfo>(),
                    CreatedAt = x.CreationTime,
                    ExpiresAt = x.CreationTime.AddSeconds(x.Lifetime),
                    Type = PersistedGrantTypes.RefreshToken
                });
            var access = grants
                .Where(x => x.Type == PersistedGrantTypes.ReferenceToken)
                .Select(x => serializer.Deserialize<Token>(x.Data))
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

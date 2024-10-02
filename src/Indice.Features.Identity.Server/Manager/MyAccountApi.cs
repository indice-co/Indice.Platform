using IdentityModel;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing a user's account.</summary>
public static class MyAccountApi
{
    /// <summary>Adds Indice Identity Server user account endpoints.</summary>
    /// <param name="routes">Indice Identity Server route builder.</param>
    public static RouteGroupBuilder MapMyAccount(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}");
        group.WithTags("MyAccount");
        group.WithGroupName("identity");
        var allowedScopes = new[] { options.ApiScope }.Where(x => x is not null).Cast<string>().ToArray();
        group.RequireAuthorization(builder => builder
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
        );
        group.WithOpenApi();
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("my/account/email", MyAccountHandlers.UpdateEmail)
             .WithName(nameof(MyAccountHandlers.UpdateEmail))
             .WithSummary("Updates the email of the current user.")
             .WithParameterValidation<UpdateUserEmailRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/email/confirmation", MyAccountHandlers.ConfirmEmail)
             .WithName(nameof(MyAccountHandlers.ConfirmEmail))
             .WithSummary("Confirms the email address of a given user.")
             .WithParameterValidation<ConfirmEmailRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/phone-number", MyAccountHandlers.UpdatePhoneNumber)
             .WithName(nameof(MyAccountHandlers.UpdatePhoneNumber))
             .WithSummary("Requests a phone number change for the current user.")
             .WithParameterValidation<UpdateUserPhoneNumberRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/phone-number/confirmation", MyAccountHandlers.ConfirmPhoneNumber)
             .WithName(nameof(MyAccountHandlers.ConfirmPhoneNumber))
             .WithSummary("Confirms the phone number of the user, using the OTP token.")
             .WithParameterValidation<ConfirmPhoneNumberRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/block", MyAccountHandlers.BlockAccount)
             .WithName(nameof(MyAccountHandlers.BlockAccount))
             .WithSummary("Blocks a user account.")
             .WithParameterValidation<SetUserBlockRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/username", MyAccountHandlers.UpdateUserName)
             .WithName(nameof(MyAccountHandlers.UpdateUserName))
             .WithSummary("Changes the username for the current user.")
             .WithParameterValidation<UpdateUserNameRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/password", MyAccountHandlers.UpdatePassword)
             .WithName(nameof(MyAccountHandlers.UpdatePassword))
             .WithSummary("Changes the password for the current user, but requires the old password to be present.")
             .WithParameterValidation<ChangePasswordRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPost("account/forgot-password", MyAccountHandlers.ForgotPassword)
             .WithName(nameof(MyAccountHandlers.ForgotPassword))
             .WithSummary("Generates a password reset token and sends it to the user via email.")
             .WithParameterValidation<ForgotPasswordRequest>()
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.ForgotPassword);

        group.MapPut("account/forgot-password/confirmation", MyAccountHandlers.ForgotPasswordConfirmation)
             .WithName(nameof(MyAccountHandlers.ForgotPasswordConfirmation))
             .WithSummary("Changes the password of the user confirming the code received during forgot password process.")
             .WithParameterValidation<ForgotPasswordConfirmationRequest>()
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.ForgotPasswordConfirmation);

        group.MapPut("my/account/password-expiration-policy", MyAccountHandlers.UpdatePasswordExpirationPolicy)
             .WithName(nameof(MyAccountHandlers.UpdatePasswordExpirationPolicy))
             .WithSummary("Updates the password expiration policy.")
             .WithParameterValidation<UpdatePasswordExpirationPolicyRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/max-devices-count", MyAccountHandlers.UpdateMaxDevicesCount)
             .WithName(nameof(MyAccountHandlers.UpdateMaxDevicesCount))
             .WithSummary("Updates the max devices count.")
             .WithParameterValidation<UpdateMaxDevicesCountRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapGet("my/account/claims", MyAccountHandlers.GetClaims)
             .WithName(nameof(MyAccountHandlers.GetClaims))
             .WithSummary("Gets the claims of the user.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapGet("my/account/grants", MyAccountHandlers.GetConsents)
             .WithName(nameof(MyAccountHandlers.GetConsents))
             .WithSummary("Gets the consents given by the user.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPost("my/account/claims", MyAccountHandlers.AddClaims)
             .WithName(nameof(MyAccountHandlers.AddClaims))
             .WithSummary("Adds the requested claims on the current user's account.")
             .WithParameterValidation<IEnumerable<CreateClaimRequest>>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPatch("my/account/claims", MyAccountHandlers.PatchClaims)
             .WithName(nameof(MyAccountHandlers.PatchClaims))
             .WithSummary("Upserts the requested claims on the current user's account.")
             .WithParameterValidation<IEnumerable<CreateClaimRequest>>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/account/claims/{claimId:int}", MyAccountHandlers.UpdateClaim)
             .WithName(nameof(MyAccountHandlers.UpdateClaim))
             .WithSummary("Updates the specified claim for the current user.")
             .WithParameterValidation<UpdateUserClaimRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapDelete("my/account", MyAccountHandlers.DeleteAccount)
             .WithName(nameof(MyAccountHandlers.DeleteAccount))
             .WithSummary("Permanently deletes current user's account.")
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapGet("account/password-options", MyAccountHandlers.GetPasswordOptions)
             .WithName(nameof(MyAccountHandlers.GetPasswordOptions))
             .WithSummary("Gets the password options that are applied when the user creates an account.")
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.PasswordOptions);

        group.MapPost("account/username-exists", MyAccountHandlers.CheckUserNameExists)
             .WithName(nameof(MyAccountHandlers.CheckUserNameExists))
             .WithSummary("Checks if a username already exists in the database.")
             .WithParameterValidation<ValidateUserNameRequest>()
             .ProducesProblem(StatusCodes.Status410Gone)
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.UserNameExists);

        group.MapPost("account/validate-password", MyAccountHandlers.ValidatePassword)
             .WithName(nameof(MyAccountHandlers.ValidatePassword))
             .WithSummary($"Validates a user's password against one or more configured {nameof(IPasswordValidator<User>)}.")
             .WithParameterValidation<ValidatePasswordRequest>()
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.ValidatePassword);

        group.MapPost("account/register", MyAccountHandlers.Register)
             .WithName(nameof(MyAccountHandlers.Register))
             .WithSummary("Self-service user registration endpoint.")
             .WithParameterValidation<RegisterRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapGet("account/calling-codes", MyAccountHandlers.GetSupportedCallingCodes)
             .WithName(nameof(MyAccountHandlers.GetSupportedCallingCodes))
             .WithSummary("Retrieves the supported calling codes.")
             .AllowAnonymous()
             .RequireRateLimiting(IdentityEndpoints.RateLimiter.Policies.CallingCodes);

        /* The avatar must be stored inside the UserClaims Table with the claim type JwtClaimTypes.Picture *
         * the value format is the binary uri: data:image/png;ivBOR.........                               *
         * The response must be cahed and taged so it can be invalidated.
         * The update my avatar should invalidate the cache.
         * The update my avatar should validate size file type, and resize the bounding box with max side size to sqare. 
         * The update avatar should do the convert to base64 and store
         * All get avatar calls should be able to resize to spesific sizes 24,32,64,128
         * The get avatar by id should be conditionally exposed ExtendedEndpointOptions.
        group.MapGet("my/avatar", MyAccountHandlers.GetMyAvatar)
             .WithName(nameof(MyAccountHandlers.UpdateEmail))
             .WithSummary("Updates the email of the current user.")
             .WithParameterValidation<UpdateUserEmailRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("my/avatar", MyAccountHandlers.UpdateMyAvatar)
             .WithName(nameof(MyAccountHandlers.UpdateEmail))
             .WithSummary("Updates the email of the current user.")
             .WithParameterValidation<UpdateUserEmailRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.MapPut("avatar/{userId}", MyAccountHandlers.GetUserAvatar)
             .WithName(nameof(MyAccountHandlers.UpdateEmail))
             .WithSummary("Updates the email of the current user.")
             .WithParameterValidation<UpdateUserEmailRequest>()
             .AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        */
        return group;
    }
}

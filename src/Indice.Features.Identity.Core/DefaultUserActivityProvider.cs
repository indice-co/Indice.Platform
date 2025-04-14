using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.IdentityValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core;

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{User}"/> and <see cref="ExtendedSignInManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="DefaultUserActivityProvider{User}"/>.</remarks>
public class DefaultUserActivityProvider() : DefaultUserActivityProvider<User>
{ }

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</summary>
public class DefaultUserActivityProvider<TUser> : IUserActivityProvider<TUser> where TUser : User
{

    /// <summary>Creates a new instance of <see cref="DefaultUserActivityProvider{TUser}"/>.</summary>
    public DefaultUserActivityProvider() {
        
    }

    /// <inheritdoc/>
    public async Task<UserActivityRequirement> GetNextAsync(HttpContext httpContext, TUser user) {
        var validators = httpContext.RequestServices.GetServices<IIdentityValidationActivity>().ToList();
        for(var i = 0; i < validators.Count; i++) {
            if (i < validators.Count - 1) {
                validators[i].Next = validators[i + 1];
            }
        }
        var context = new UserValidationActivityContext(user, httpContext);
        var start = validators[0];
        await start.HandleAsync(context);
        return context.Result?.Requirement ?? UserActivityRequirement.None; 
    }

    /* Note for future self: Never change the order of the combinations in the method below. It does matter.
    private async Task<UserState> GetNextStateAsync(TUser user, UserAction action) => (CurrentState, action) switch {
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == true &&
                                                     user.PhoneNumberConfirmed == false &&
                                                     (await _httpContext!.RequestServices.GetRequiredService<IAuthenticationMethodProvider>().GetRequiredAuthenticationMethod(user))?.GetType() == typeof(SmsAuthenticationMethod) &&
                                                     (await _httpContext.RequestServices.GetRequiredService<ExtendedSignInManager<TUser>>().IsTwoFactorClientRememberedAsync(user)) => throw new InvalidOperationException("User cannot have MFA enabled without a verified phone number."),
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == false &&
                                                    _mfaPolicy == MfaPolicy.Enforced => UserState.RequiresMfaOnboarding,
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == true => UserState.RequiresMfa,
        (UserState.LoggedOut, UserAction.Login) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.LoggedOut, UserAction.Login) when user.EmailConfirmed == false &&
                                                     _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.LoggedOut, UserAction.Login) when user.PhoneNumberConfirmed == false && _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.LoggedOut, UserAction.Login) => UserState.LoggedIn,
        (UserState.LoggedOut, UserAction.ExternalLogin) when user.TwoFactorEnabled == true &&
                                                             user.PhoneNumberConfirmed == false => throw new InvalidOperationException("User cannot have MFA enabled without a verified phone number."),
        (UserState.LoggedOut, UserAction.ExternalLogin) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.LoggedOut, UserAction.ExternalLogin) when user.EmailConfirmed == false &&
                                                             _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.LoggedOut, UserAction.ExternalLogin) when user.PhoneNumberConfirmed == false &&
                                                             _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.LoggedOut, UserAction.ExternalLogin) => UserState.LoggedIn,
        (UserState.RequiresMfaOnboarding, UserAction.VerifiedPhoneNumber) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresMfaOnboarding, UserAction.VerifiedPhoneNumber) when user.EmailConfirmed == false &&
                                                                               _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresMfaOnboarding, UserAction.MfaEnabled) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresMfaOnboarding, UserAction.MfaEnabled) when user.EmailConfirmed == false &&
                                                                      _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresMfaOnboarding, UserAction.MfaEnabled) => UserState.LoggedIn,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) when user.EmailConfirmed == false &&
                                                                          _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) when user.PhoneNumberConfirmed == false &&
                                                                          _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) => UserState.LoggedIn,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) when user.EmailConfirmed == false &&
                                                                                         _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) when user.EmailConfirmed == true => UserState.LoggedIn,
        (UserState.RequiresEmailVerification, UserAction.VerifiedEmail) when user.PhoneNumberConfirmed == false &&
                                                                             _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.RequiresEmailVerification, UserAction.VerifiedEmail) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresEmailVerification, UserAction.VerifiedEmail) => UserState.LoggedIn,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) when user.PhoneNumberConfirmed == false &&
                                                                            _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) when user.EmailConfirmed == false &&
                                                                       _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) => UserState.LoggedIn,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) => UserState.LoggedIn,
        (UserState.LoggedIn, UserAction.Logout) => UserState.LoggedOut,
        _ => CurrentState
    }; 
     */
}

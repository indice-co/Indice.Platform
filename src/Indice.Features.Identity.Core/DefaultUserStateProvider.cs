﻿using System.Text;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core;

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{User}"/> and <see cref="ExtendedSignInManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="DefaultUserStateProvider{User}"/>.</remarks>
/// <param name="configuration">Represents a set of key/value application configuration properties.</param>
/// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
public class DefaultUserStateProvider(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : DefaultUserStateProvider<User>(configuration, httpContextAccessor) { }

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</summary>
public class DefaultUserStateProvider<TUser> : IUserStateProvider<TUser> where TUser : User
{
    private readonly bool _requirePostSignInConfirmedEmail;
    private readonly bool _requirePostSignInConfirmedPhoneNumber;
    private readonly MfaPolicy _mfaPolicy;
    private readonly HttpContext? _httpContext;
    private const string USER_LOGIN_STATE_SESSION_KEY = "user_login_state";

    /// <summary>Creates a new instance of <see cref="DefaultUserStateProvider{TUser}"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    public DefaultUserStateProvider(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    ) {
        _requirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedEmail)) == true;
        _requirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedPhoneNumber)) == true;
        _mfaPolicy = configuration.GetIdentityOption<MfaPolicy?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "Policy") ?? MfaPolicy.Default;
        _httpContext = httpContextAccessor.HttpContext;
        if (_httpContext is not null && _httpContext.Session.TryGetValue(USER_LOGIN_STATE_SESSION_KEY, out var bytes) == true) {
            CurrentState = Enum.Parse<UserState>(Encoding.UTF8.GetString(bytes));
        }
    }

    /// <inheritdoc />
    public UserState CurrentState { get; private set; }

    /// <inheritdoc />
    public async Task ChangeStateAsync(TUser user, UserAction action) {
        CurrentState = await GetNextStateAsync(user, action);
        _httpContext?.Session.Set(USER_LOGIN_STATE_SESSION_KEY, Encoding.UTF8.GetBytes(CurrentState.ToString()));
    }

    /// <inheritdoc />
    public void ClearState() => _httpContext?.Session.Clear();

    /* Note for future self: Never change the order of the combinations in the method below. It does matter. */
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
}

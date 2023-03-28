using System.Text;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</summary>
public class UserStateProvider
{
    private readonly bool _requirePostSignInConfirmedEmail;
    private readonly bool _requirePostSignInConfirmedPhoneNumber;
    private readonly HttpContext _httpContext;
    private const string USER_LOGIN_STATE_SESSION_KEY = "user_login_state";

    /// <summary>Creates a new instance of <see cref="UserStateProvider"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="httpContextAccessor"></param>
    public UserStateProvider(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    ) {
        _requirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedEmail)) == true;
        _requirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedPhoneNumber)) == true;
        MfaPolicy = configuration.GetIdentityOption<MfaPolicy?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "Policy") ?? MfaPolicy.Default;
        _httpContext = httpContextAccessor?.HttpContext;
        if (_httpContext?.Session.TryGetValue(USER_LOGIN_STATE_SESSION_KEY, out var bytes) == true) {
            CurrentState = Enum.Parse<UserState>(Encoding.UTF8.GetString(bytes));
        }
    }

    /// <summary>Current user login state value.</summary>
    public UserState CurrentState { get; private set; }
    /// <summary>MFA policy applied for new users.</summary>
    private MfaPolicy MfaPolicy { get; }

    internal void ChangeStateTo(UserAction action, User user) {
        CurrentState = GetNextState(action, user);
        _httpContext?.Session.Set(USER_LOGIN_STATE_SESSION_KEY, Encoding.UTF8.GetBytes(CurrentState.ToString()));
    }

    internal void Clear() => _httpContext?.Session.Clear();

    private UserState GetNextState(UserAction action, User user) => (CurrentState, action) switch {
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == false && MfaPolicy == MfaPolicy.Enforced => UserState.RequiresMfaOnboarding,
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == true && user.PhoneNumberConfirmed == false => throw new InvalidOperationException("User cannot have MFA enabled without a verified phone number."),
        (UserState.LoggedOut, UserAction.Login) when user.TwoFactorEnabled == true => UserState.RequiresMfa,
        (UserState.LoggedOut, UserAction.Login) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.LoggedOut, UserAction.Login) when user.EmailConfirmed == false && _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.LoggedOut, UserAction.Login) when user.PhoneNumberConfirmed == false && _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.LoggedOut, UserAction.Login) => UserState.LoggedIn,
        (UserState.RequiresMfaOnboarding, UserAction.MfaEnabled) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) when user.HasExpiredPassword() == true => UserState.RequiresPasswordChange,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) when user.EmailConfirmed == false && _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresMfa, UserAction.MultiFactorAuthenticated) => UserState.LoggedIn,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) when user.EmailConfirmed == false && _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) when user.EmailConfirmed == true => UserState.LoggedIn,
        (UserState.RequiresEmailVerification, UserAction.VerifiedEmail) when user.PhoneNumberConfirmed == false && _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.RequiresEmailVerification, UserAction.VerifiedEmail) => UserState.LoggedIn,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) when user.PhoneNumberConfirmed == false && _requirePostSignInConfirmedPhoneNumber => UserState.RequiresPhoneNumberVerification,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) when user.EmailConfirmed == false && _requirePostSignInConfirmedEmail => UserState.RequiresEmailVerification,
        (UserState.RequiresPasswordChange, UserAction.PasswordChanged) => UserState.LoggedIn,
        (UserState.RequiresPhoneNumberVerification, UserAction.VerifiedPhoneNumber) => UserState.LoggedIn,
        (UserState.LoggedIn, UserAction.Logout) => UserState.LoggedOut,
        _ => CurrentState
    };
}

/// <summary>Describes the state of the current principal.</summary>
public enum UserState
{
    /// <summary>Logged out.</summary>
    LoggedOut,
    /// <summary>Logged in.</summary>
    LoggedIn,
    /// <summary>Requires phone number verification.</summary>
    RequiresPhoneNumberVerification,
    /// <summary>Requires email verification.</summary>
    RequiresEmailVerification,
    /// <summary>Requires password change.</summary>
    RequiresPasswordChange,
    /// <summary>Requires MFA.</summary>
    RequiresMfa,
    /// <summary>MFA on-boarding.</summary>
    RequiresMfaOnboarding
}

/// <summary>Describes the action taken by the principal that changes the state.</summary>
public enum UserAction
{
    /// <summary>Login</summary>
    Login,
    /// <summary>Verified phone number.</summary>
    VerifiedPhoneNumber,
    /// <summary>Verified email.</summary>
    VerifiedEmail,
    /// <summary>Passed MFA.</summary>
    MultiFactorAuthenticated,
    /// <summary>Changed password.</summary>
    PasswordChanged,
    /// <summary>MFA enabled.</summary>
    MfaEnabled,
    /// <summary>Logout.</summary>
    Logout
}
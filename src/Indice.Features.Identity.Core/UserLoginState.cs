using System.Text;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</summary>
public class UserLoginStateService
{
    private readonly bool _requirePostSignInConfirmedEmail;
    private readonly bool _requirePostSignInConfirmedPhoneNumber;
    private readonly HttpContext _httpContext;
    private const string USER_LOGIN_STATE_SESSION_KEY = "user_login_state";

    /// <summary>Creates a new instance of <see cref="UserLoginStateService"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="httpContextAccessor"></param>
    public UserLoginStateService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    ) {
        _requirePostSignInConfirmedEmail = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedEmail)) == true;
        _requirePostSignInConfirmedPhoneNumber = configuration.GetIdentityOption<bool?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.RequirePostSignInConfirmedPhoneNumber)) == true;
        _httpContext = httpContextAccessor?.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        if (_httpContext.Session.TryGetValue(USER_LOGIN_STATE_SESSION_KEY, out var bytes)) {
            CurrentState = Enum.Parse<UserLoginState>(Encoding.UTF8.GetString(bytes));
        }
    }

    /// <summary>Current user login state value.</summary>
    public UserLoginState CurrentState { get; private set; }

    internal void ChangeStateTo(UserLoginAction action, User user = null) {
        CurrentState = (CurrentState, action) switch {
            (UserLoginState.LoggedOut, UserLoginAction.Login) when user?.TwoFactorEnabled == true && user?.HasExpiredPassword() == true => UserLoginState.RequiresMfa,
            (UserLoginState.LoggedOut, UserLoginAction.Login) when user?.TwoFactorEnabled == false && user?.HasExpiredPassword() == true => UserLoginState.RequiresPasswordChange,
            (UserLoginState.LoggedOut, UserLoginAction.Login) when user?.PhoneNumberConfirmed == false && _requirePostSignInConfirmedPhoneNumber => UserLoginState.RequiresPhoneNumberVerification,
            (UserLoginState.LoggedOut, UserLoginAction.Login) when user?.EmailConfirmed == false && _requirePostSignInConfirmedEmail => UserLoginState.RequiresEmailVerification,
            (UserLoginState.LoggedOut, UserLoginAction.Login) when user?.TwoFactorEnabled == true => UserLoginState.RequiresMfa,
            (UserLoginState.LoggedOut, UserLoginAction.Login) => UserLoginState.LoggedIn,
            (UserLoginState.RequiresMfa, UserLoginAction.MultiFactorAuthenticated) when user?.HasExpiredPassword() == true => UserLoginState.RequiresPasswordChange,
            (UserLoginState.RequiresMfa, UserLoginAction.MultiFactorAuthenticated) => UserLoginState.LoggedIn,
            (UserLoginState.RequiresPasswordChange, UserLoginAction.PasswordChanged) => UserLoginState.LoggedIn,
            (UserLoginState.LoggedIn, UserLoginAction.Logout) => UserLoginState.LoggedOut,
            _ => throw new InvalidOperationException()
        };
        _httpContext.Session.Set(USER_LOGIN_STATE_SESSION_KEY, Encoding.UTF8.GetBytes(CurrentState.ToString()));
    }
}

/// <summary>Describes the state of the current principal.</summary>
public enum UserLoginState
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
    RequiresMfa
}

/// <summary>Describes the action taken by the principal that changes the state.</summary>
public enum UserLoginAction
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
    /// <summary>Logout.</summary>
    Logout
}
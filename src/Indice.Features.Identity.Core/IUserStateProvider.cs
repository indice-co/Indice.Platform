using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core;

/// <summary></summary>
public interface IUserStateProvider<TUser> where TUser : User
{
    /// <summary>Current user login state value.</summary>
    UserState CurrentState { get; }
    /// <summary></summary>
    /// <param name="user"></param>
    /// <param name="action"></param>
    void ChangeState(TUser user, UserAction action);
    /// <summary>Clears the current state.</summary>
    void ClearState();
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
public enum UserAction : byte
{
    /// <summary>Login</summary>
    Login,
    /// <summary>Verified phone number</summary>
    VerifiedPhoneNumber,
    /// <summary>Verified email</summary>
    VerifiedEmail,
    /// <summary>Passed MFA</summary>
    MultiFactorAuthenticated,
    /// <summary>Changed password</summary>
    PasswordChanged,
    /// <summary>MFA enabled</summary>
    MfaEnabled,
    /// <summary>Logout</summary>
    Logout,
    /// <summary>External login</summary>
    ExternalLogin
}

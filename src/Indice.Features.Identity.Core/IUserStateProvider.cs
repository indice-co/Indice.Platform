using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core;

/// <summary>Manages the user state during login process.</summary>
public interface IUserStateProvider<TUser> where TUser : User
{
    /// <summary>Current user login state value.</summary>
    UserState CurrentState { get; }
    /// <summary></summary>
    /// <param name="user"></param>
    /// <param name="action"></param>
    Task ChangeStateAsync(TUser user, UserAction action);
    /// <summary>Clears the current state.</summary>
    void ClearState();
}

/// <summary>Extension methods on <see cref="IUserStateProvider{TUser}"/> interface.</summary>
public static class IUserStateProviderExtensions 
{
    /// <summary>Checks whether the user should be redirected to extended validation.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <param name="userStateProvider">Manages the user state during login process.</param>
    public static bool ShouldSignInForExtendedValidation<TUser>(this IUserStateProvider<TUser> userStateProvider) where TUser : User =>
        userStateProvider.CurrentState == UserState.RequiresEmailVerification ||
        userStateProvider.CurrentState == UserState.RequiresPhoneNumberVerification ||
        userStateProvider.CurrentState == UserState.RequiresPasswordChange;

    /// <summary>Checks whether the user should be redirected to MFA on-boarding process.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <param name="userStateProvider">Manages the user state during login process.</param>
    public static bool ShouldSignInForMfaOnboarding<TUser>(this IUserStateProvider<TUser> userStateProvider) where TUser : User => 
        userStateProvider.CurrentState == UserState.RequiresMfaOnboarding;

    /// <summary>Encapsulates both <see cref="ShouldSignInForExtendedValidation{TUser}(IUserStateProvider{TUser})"/> and <see cref="ShouldSignInForMfaOnboarding{TUser}(IUserStateProvider{TUser})"/> methods.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <param name="userStateProvider">Manages the user state during login process.</param>
    public static bool ShouldSignInPartially<TUser>(this IUserStateProvider<TUser> userStateProvider) where TUser : User => 
        userStateProvider.ShouldSignInForExtendedValidation() || 
        userStateProvider.ShouldSignInForMfaOnboarding();
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
    RequiresMfaOnboarding,
    /// <summary>Is impossible traveler.</summary>
    IsImpossibleTraveler
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

using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>Manages the user state during login process.</summary>
public interface IUserActivityProvider<TUser> where TUser : User
{
    /// <summary></summary>
    /// <param name="httpContext"></param>
    /// <param name="user"></param>
    Task<UserActivityRequirement> GetNextAsync(HttpContext httpContext, TUser user);
}

/// <summary>Extension methods on <see cref="IUserActivityProvider{TUser}"/> interface.</summary>
public static class IUserStateProviderExtensions 
{

}

/// <summary>
/// User activity requirement.
/// </summary>
/// <param name="Kind"></param>
/// <param name="PageName"></param>
public record UserActivityRequirement(UserActivityRequirementKind Kind, string? PageName) { 
    private static UserActivityRequirement _None = new UserActivityRequirement(UserActivityRequirementKind.None, null);
    /// <summary>None.</summary>
    public static UserActivityRequirement None => _None;
}

/// <summary>Describes the required validation activity needed to be executed on the current principal while partially logged in, before he can proceed with the full login.</summary>
public enum UserActivityRequirementKind
{
    /// <summary>None.</summary>
    None,
    /// <summary>Requires phone number verification.</summary>
    RequiresPhoneNumberVerification,
    /// <summary>Requires email verification.</summary>
    RequiresEmailVerification,
    /// <summary>Requires password change.</summary>
    RequiresPasswordChange,
    /// <summary>MFA on-boarding.</summary>
    RequiresMfaOnboarding
}
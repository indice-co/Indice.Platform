using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.IdentityValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Core;

/// <summary>Manages the user state during login process.</summary>
public interface IUserRequirementProvider<TUser> where TUser : User
{
    /// <summary>
    /// Gets the next user activity requirement based on the current HTTP context and user.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="user"></param>
    Task<UserValidationRequirement> GetNextAsync(HttpContext httpContext, TUser user);
}

/// <summary>Extension methods on <see cref="IUserRequirementProvider{TUser}"/> interface.</summary>
public static class IUserRequirementProviderExtensions
{
    /// <summary>
    /// Checks if the user activity provider requires validation for the given user.
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <param name="provider">The provider to extend.</param>
    /// <param name="httpContext">The HttpContext.</param>
    /// <param name="user">the user to check.</param>
    /// <returns>True if the user requires extended validation. Otherwize false.</returns>
    public static async Task<bool> RequiresValidationAsync<TUser>(this IUserRequirementProvider<TUser> provider, HttpContext httpContext, TUser user) where TUser : User {
        var requirement = await provider.GetNextAsync(httpContext, user);
        return requirement.Kind != UserActivityRequirementKind.None;
    }
}

/// <summary>
/// User activity requirement.
/// </summary>
/// <param name="Kind"></param>
/// <param name="PageName"></param>
public record UserValidationRequirement(UserActivityRequirementKind Kind, string? PageName) { 
    private static readonly UserValidationRequirement _None = new UserValidationRequirement(UserActivityRequirementKind.None, null);
    /// <summary>None.</summary>
    public static UserValidationRequirement None => _None;
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


/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{User}"/> and <see cref="ExtendedSignInManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="DefaultUserRequirementProvider{User}"/>.</remarks>
public class DefaultUserRequirementProvider() : DefaultUserRequirementProvider<User>
{ }

/// <summary>A service used to implement state machine for <see cref="ExtendedUserManager{TUser}"/> and <see cref="ExtendedSignInManager{TUser}"/>.</summary>
public class DefaultUserRequirementProvider<TUser> : IUserRequirementProvider<TUser> where TUser : User
{

    /// <summary>Creates a new instance of <see cref="DefaultUserRequirementProvider{TUser}"/>.</summary>
    public DefaultUserRequirementProvider() {

    }

    /// <inheritdoc/>
    public async Task<UserValidationRequirement> GetNextAsync(HttpContext httpContext, TUser user) {
        var validators = httpContext.RequestServices.GetServices<IIdentityValidationActivity>().ToList();
        for (var i = 0; i < validators.Count; i++) {
            if (i < validators.Count - 1) {
                validators[i].Next = validators[i + 1];
            }
        }
        var context = new UserValidationActivityContext(user, httpContext);
        var start = validators[0];
        await start.HandleAsync(context);
        return context.Result?.Requirement ?? UserValidationRequirement.None;
    }
}

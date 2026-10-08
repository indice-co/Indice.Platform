using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;

namespace Indice.Features.Identity.Core.Extensions;

/// <summary>Extension methods on <see cref="User"/> type.</summary>
public static class UserExtensions
{
    /// <summary>Finds a display name for the user based on <see cref="BasicClaimTypes.Name"/>, <see cref="BasicClaimTypes.GivenName"/>, <see cref="BasicClaimTypes.FamilyName"/> claims or user email.</summary>
    /// <param name="user">The user instance.</param>
    public static string? FindDisplayName(this User user) {
        var displayName = default(string);
        var name = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.Name)?.ClaimValue;
        var firstName = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.GivenName)?.ClaimValue;
        var lastName = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.FamilyName)?.ClaimValue;
        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)) {
            displayName = $"{firstName} {lastName}".Trim();
        } else if (!string.IsNullOrEmpty(name)) {
            displayName = name;
        } else if (!string.IsNullOrEmpty(user.Email)) {
            displayName = user.Email;
        }
        return displayName;
    }

    /// <summary>
    /// Finds user initials based on <see cref="BasicClaimTypes.Name"/>, <see cref="BasicClaimTypes.GivenName"/>, <see cref="BasicClaimTypes.FamilyName"/> claims or user email. 
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>The initials of the user.</returns>
    public static string? FindInitials(this User user) {
        var initials = default(string);
        var name = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.Name)?.ClaimValue;
        var firstName = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.GivenName)?.ClaimValue;
        var lastName = user.Claims.FirstOrDefault(x => x.ClaimType is BasicClaimTypes.FamilyName)?.ClaimValue;
        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName)) {
            initials = $"{firstName?[0]}{lastName?[0]}".Trim();
        } else if (!string.IsNullOrEmpty(name)) {
            initials = $"{name[0]}";
        } else if (!string.IsNullOrEmpty(user.Email)) {
            initials = $"{user.Email[0]}";
        }
        return initials;
    }
}

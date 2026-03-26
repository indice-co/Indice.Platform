using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;

namespace Indice.Features.Identity.Core.Extensions;

/// <summary>Extension methods on <see cref="User"/> type.</summary>
public static class UserExtensions
{
    /// <summary>Finds a display name for the user based on <see cref="BasicClaimTypes.GivenName"/>, <see cref="BasicClaimTypes.FamilyName"/> claims or user email.</summary>
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
}

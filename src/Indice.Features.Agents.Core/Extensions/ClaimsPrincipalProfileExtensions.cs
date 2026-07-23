using System.Security.Claims;

namespace Indice.Security;

/// <summary>
/// Extension methods on <see cref="ClaimsPrincipal"/> regarding the read of profile data.
/// </summary>
public static class ClaimsPrincipalProfileExtensions
{

    /// <summary>
    /// Reads the profile claims from the <see cref="ClaimsPrincipal"/> and returns a tuple containing the subject ID, name, email, locale, and display name.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> instance from which to read the claims.</param>
    /// <returns>A tuple containing the subject ID, name, email, locale, and display name.</returns>
    public static (string subjectId, string? name, string? email, string? locale, string? displayName) ReadProfile(this ClaimsPrincipal user) {
        var name = user.FindFirst(BasicClaimTypes.Name)?.Value;
        var givenName = user.FindFirst(BasicClaimTypes.GivenName)?.Value;
        var familyName = user.FindFirst(BasicClaimTypes.FamilyName)?.Value;
        var displayName = $"{givenName} {familyName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName)) {
            displayName = name;
        }
        return (user.FindSubjectId()!,
                name,
                user.FindFirst(BasicClaimTypes.Email)?.Value,
                user.FindFirst(BasicClaimTypes.Locale)?.Value,
                displayName);
    }
}

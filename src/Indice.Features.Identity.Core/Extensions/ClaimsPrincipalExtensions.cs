using System.Security.Claims;
using Indice.Features.Identity.Core;

namespace Indice.Security;

/// <summary>Extension methods on <see cref="ClaimsPrincipal"/> type.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns true if the principal has an identity with the specified cookie scheme.</summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
    /// <param name="authenticationScheme">The authentication scheme to check.</param>
    /// <returns>True if the user is logged in with specified identity and scheme.</returns>
    public static bool IsSignedInWithScheme(this ClaimsPrincipal principal, string authenticationScheme) {
        if (principal == null) {
            throw new ArgumentNullException(nameof(principal));
        }
        return principal?.Identities != null && principal.Identities.Any(x => x.AuthenticationType == authenticationScheme); 
    }

    /// <summary>Returns true if the principal is partially signed in with the <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.</summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
    /// <returns>True if the user is logged in with specified identity and scheme.</returns>
    public static bool IsSignedInPartially(this ClaimsPrincipal principal) =>
        principal.IsSignedInWithScheme(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);

    /// <summary>Checks if the current principal can read users data.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanReadUsers(this ClaimsPrincipal principal) =>
        principal.HasRoleClaim(BasicRoleNames.AdminUIUsersReader) || principal.HasRoleClaim(BasicRoleNames.AdminUIUsersWriter) || principal.IsAdmin() || principal.IsSystemClient();

    /// <summary>Checks if the current principal can read and write users data.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanWriteUsers(this ClaimsPrincipal principal) =>
        principal.HasRoleClaim(BasicRoleNames.AdminUIUsersWriter) || principal.IsAdmin() || principal.IsSystemClient();

    /// <summary>Checks if the current principal can read clients data.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanReadClients(this ClaimsPrincipal principal) =>
        principal.HasRoleClaim(BasicRoleNames.AdminUIClientsReader) || principal.HasRoleClaim(BasicRoleNames.AdminUIClientsWriter) || principal.IsAdmin() || principal.IsSystemClient();

    /// <summary>Checks if the current principal can read and write clients data.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanWriteClients(this ClaimsPrincipal principal) =>
        principal.HasRoleClaim(BasicRoleNames.AdminUIClientsWriter) || principal.IsAdmin() || principal.IsSystemClient();

    /// <summary>Checks if the current principal can manage campaigns data.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanManageCampaigns(this ClaimsPrincipal principal) =>
        principal.HasRoleClaim(BasicRoleNames.CampaignManager) || principal.IsAdmin() || principal.IsSystemClient();
}

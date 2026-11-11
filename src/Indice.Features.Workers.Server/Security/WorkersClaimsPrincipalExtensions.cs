using System.Security.Claims;

namespace Indice.Security;

/// <summary>Checks of the workers dashboard roles on a <see cref="ClaimsPrincipal"/>.</summary>
public static class WorkersClaimsPrincipalExtensions
{
    /// <summary>Whether the principal can view the queues, scheduled tasks and locks: an admin, a <see cref="BasicRoleNames.WorkersAdmin"/> or a <see cref="BasicRoleNames.WorkersReader"/>.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanReadWorkers(this ClaimsPrincipal principal) =>
        principal.CanManageWorkers() || principal.HasRole(BasicRoleNames.WorkersReader);

    /// <summary>Whether the principal can change the queues, scheduled tasks and locks: an admin or a <see cref="BasicRoleNames.WorkersAdmin"/>.</summary>
    /// <param name="principal">The current principal.</param>
    public static bool CanManageWorkers(this ClaimsPrincipal principal) =>
        principal.IsAdmin() || principal.HasRole(BasicRoleNames.WorkersAdmin);

    // The role claim type depends on how the authentication handler maps the inbound claims, so both are checked.
    private static bool HasRole(this ClaimsPrincipal principal, string role) => principal.IsInRole(role) || principal.HasRoleClaim(role);
}

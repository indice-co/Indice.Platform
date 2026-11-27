namespace Indice.Features.Identity.Core.Data.Stores;

/// <summary>Resolves the canonical name of a role from its normalized name.</summary>
public interface IRoleNameLookup
{
    /// <summary>Finds the canonical role name, or null if the role does not exist.</summary>
    /// <param name="normalizedRoleName">The normalized role name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<string?> FindRoleNameAsync(string normalizedRoleName, CancellationToken cancellationToken);
}

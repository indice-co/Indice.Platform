using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>
/// Resolves the permission a customer has on a case as its owner, co-owner or creator.
/// The answer is cached per user and case, so it must be invalidated when the owner or the co-owners of the case change.
/// </summary>
public interface ICaseContactPermissionService
{
    /// <summary>Gets the permission of a customer on a case.</summary>
    /// <param name="userId">The id of the customer.</param>
    /// <param name="caseId">The id of the case.</param>
    /// <returns>The permission, or null when the customer takes no part in the case or the case does not exist.</returns>
    Task<ContactPermission?> GetPermission(string userId, Guid caseId);

    /// <summary>Forgets the cached permission of a customer on a case.</summary>
    /// <param name="userId">The id of the customer.</param>
    /// <param name="caseId">The id of the case.</param>
    Task Invalidate(string userId, Guid caseId);
}

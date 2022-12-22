using System.Security.Claims;
using Indice.Features.Cases.Models;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The case action interface.
    /// </summary>
    internal interface ICaseActionsService
    {
        /// <summary>
        /// Get the actions a user is allowed to perform, based on its role, to a case.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="caseId">The Id of the case.</param>
        /// <returns></returns>
        ValueTask<CaseActions> GeUserActions(ClaimsPrincipal user, Guid caseId);
    }
}
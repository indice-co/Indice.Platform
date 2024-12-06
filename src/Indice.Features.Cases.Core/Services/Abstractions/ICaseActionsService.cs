using System.Security.Claims;
using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The case action interface.</summary>
public interface ICaseActionsService
{
    /// <summary>Get the actions a user is allowed to perform, based on its role, to a case.</summary>
    /// <param name="user">The user.</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns></returns>
    Task<CaseActions?> GetUserActions(ClaimsPrincipal user, Guid caseId);
}
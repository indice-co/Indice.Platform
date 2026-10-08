using System.Security.Claims;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Server.Endpoints;

namespace Indice.Features.Agents.Server.Services;

/// <summary>Orchestrates the caller's application-local profile: JIT provisioning and preference updates.</summary>
public interface IMyProfileService
{
    /// <summary>Returns the caller's profile, provisioning/refreshing it from the principal's claims (JIT).</summary>
    Task<Profile> GetMeAsync(ClaimsPrincipal user, CancellationToken cancellationToken);

    /// <summary>Updates the caller's app-specific preferences (provisioning the row first if needed).</summary>
    Task<Profile> UpdateMeAsync(ClaimsPrincipal user, UpdateUserRequest request, CancellationToken cancellationToken);
}

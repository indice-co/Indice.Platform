using System.Security.Claims;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

/// <summary>Logic-free handlers for the MyProfileApi.</summary>
internal static class MyProfileHandlers
{
    /// <summary>GET /api/my/profile — returns the caller's profile, provisioning it on first access.</summary>
    public static async Task<Ok<Profile>> GetMe(ClaimsPrincipal user, IMyProfileService profileService, CancellationToken cancellationToken)
        => TypedResults.Ok(await profileService.GetMeAsync(user, cancellationToken));

    /// <summary>PUT /api/my/profile — updates the caller's app-specific preferences.</summary>
    public static async Task<Ok<Profile>> UpdateMe(UpdateUserRequest request, ClaimsPrincipal user, IMyProfileService profileService, CancellationToken cancellationToken)
        => TypedResults.Ok(await profileService.UpdateMeAsync(user, request, cancellationToken));
}

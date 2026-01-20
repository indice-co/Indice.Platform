using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Interface for resolving the user ID from the HTTP context and claims principal.
/// </summary>
public interface ISignalRProxyUserIdResolver
{
    /// <summary>
    /// Resolves the user ID from the HTTP context and claims principal.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="user">The claims principal of the current user.</param>
    /// <returns>The resolved user ID, or null if the user ID cannot be resolved.</returns>
    string? Resolve(HttpContext httpContext, ClaimsPrincipal user);
}

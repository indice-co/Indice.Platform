using System.Security.Claims;
using Indice.Security;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Default implementation of <see cref="ISignalRProxyUserIdResolver"/> that resolves the user ID from the "sub" claim.
/// </summary>
public class DefaultSignalRProxyUserIdResolver : ISignalRProxyUserIdResolver
{
    /// <inheritdoc />
    public string? Resolve(HttpContext httpContext, ClaimsPrincipal user) => user.FindSubjectId();
}

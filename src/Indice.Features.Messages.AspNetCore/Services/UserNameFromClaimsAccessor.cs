using System.Security.Claims;
using IdentityModel;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Messages.AspNetCore.Services;

/// <summary>An implementation of <see cref="IUserNameAccessor"/> that resolves the username using user claims.</summary>
/// <remarks>Creates a new instance of <see cref="UserNameFromClaimsAccessor"/>.</remarks>
/// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
public class UserNameFromClaimsAccessor(IHttpContextAccessor httpContextAccessor) : IUserNameAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public int Priority => 1;

    /// <inheritdoc />
    public string? Resolve() {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null) {
            return default;
        }
        return principal.FindFirstValue(JwtClaimTypes.Name)
            ?? principal.FindFirstValue(JwtClaimTypes.Email)
            ?? principal.FindFirstValue(JwtClaimTypes.ClientId)
            ?? "system";
    }
}

using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Media.AspNetCore.Services;
/// <summary>An implementation of <see cref="IUserNameAccessor"/> that resolves the username using user claims.</summary>
public class UserNameFromHttpContextAccessor : IUserNameAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="UserNameFromHttpContextAccessor"/>.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
    public UserNameFromHttpContextAccessor(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public string Resolve() {
        var principal = _httpContextAccessor.HttpContext?.User;
        return principal?.FindFirstValue(JwtClaimTypes.Name)
            ?? principal?.FindFirstValue(JwtClaimTypes.Email)
            ?? principal?.FindFirstValue(JwtClaimTypes.ClientId)
            ?? "system";
    }
}

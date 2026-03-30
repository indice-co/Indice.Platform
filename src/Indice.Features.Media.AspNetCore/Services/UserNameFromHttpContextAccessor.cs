using System.Security.Claims;
using Indice.Security;
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
        return principal?.FindFirstValue(BasicClaimTypes.Name)
            ?? principal?.FindFirstValue(BasicClaimTypes.Email)
            ?? principal?.FindFirstValue(BasicClaimTypes.ClientId)
            ?? "system";
    }
}

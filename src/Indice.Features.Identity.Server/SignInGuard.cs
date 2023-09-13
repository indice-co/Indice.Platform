using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.ImpossibleTravel.Services;

namespace Indice.Features.Identity.Server;

/// <summary>Default implementation of <see cref="ISignInGuard{TUser}"/> interface.</summary>
public class SignInGuard<TUser> : ISignInGuard<TUser> where TUser : User
{
    private readonly ImpossibleTravelDetector<TUser>? _impossibleTravelDetector;

    /// <summary>Creates a new instance of <see cref="SignInGuard{TUser}"/> class.</summary>
    /// <param name="impossibleTravelDetector">A service that detects whether a login attempt is made from an impossible location.</param>
    public SignInGuard(ImpossibleTravelDetector<TUser>? impossibleTravelDetector = null) {
        _impossibleTravelDetector = impossibleTravelDetector;
    }

    /// <inheritdoc />
    public async Task<bool> IsSuspiciousLogin(TUser user) {
        var isSuspiciousLogin = false;
        if (_impossibleTravelDetector is not null) {
            isSuspiciousLogin = await _impossibleTravelDetector.IsImpossibleTravelLogin(user);
        }
        return isSuspiciousLogin;
    }
}

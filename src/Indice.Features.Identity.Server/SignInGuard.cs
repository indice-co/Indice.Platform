using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;

namespace Indice.Features.Identity.Server;

/// <summary>Default implementation of <see cref="ISignInGuard{TUser}"/> interface.</summary>
public class SignInGuard<TUser> : ISignInGuard<TUser> where TUser : User
{
    /// <summary>Creates a new instance of <see cref="SignInGuard{TUser}"/> class.</summary>
    /// <param name="impossibleTravelDetector">A service that detects whether a login attempt is made from an impossible location.</param>
    public SignInGuard(IImpossibleTravelDetector<TUser>? impossibleTravelDetector = null) {
        ImpossibleTravelDetector = impossibleTravelDetector;
    }

    /// <inheritdoc />
    public IImpossibleTravelDetector<TUser>? ImpossibleTravelDetector { get; init; }

    /// <inheritdoc />
    public async Task<SignInGuardResult> IsSuspiciousLogin(TUser user) {
        if (ImpossibleTravelDetector is not null) {
            var isImpossibleTravel = await ImpossibleTravelDetector.IsImpossibleTravelLogin(user);
            if (isImpossibleTravel) {
                return SignInGuardResult.Failed(SignInWarning.ImpossibleTravel);
            }
        }
        return SignInGuardResult.Success();
    }
}

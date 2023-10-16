using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="ISignInGuard{TUser}"/> interface.</summary>
public class SignInGuard<TUser> : ISignInGuard<TUser> where TUser : User
{
    private static readonly string HTTP_CONTEXT_ITEM_KEY = $"{nameof(SignInGuard<TUser>)}:Result";

    /// <summary>Creates a new instance of <see cref="SignInGuard{TUser}"/> class.</summary>
    /// <param name="impossibleTravelDetector">A service that detects whether a login attempt is made from an impossible location.</param>
    public SignInGuard(IImpossibleTravelDetector<TUser> impossibleTravelDetector = null) {
        ImpossibleTravelDetector = impossibleTravelDetector;
    }

    /// <inheritdoc />
    public IImpossibleTravelDetector<TUser> ImpossibleTravelDetector { get; init; }

    /// <inheritdoc />
    public async Task<SignInGuardResult> IsSuspiciousLogin(HttpContext httpContext, TUser user) {
        if (httpContext is null) {
            throw new ArgumentNullException(nameof(httpContext));
        }
        object impossibleTravelObject = null;
        var resultExists = httpContext?.Items.TryGetValue(HTTP_CONTEXT_ITEM_KEY, out impossibleTravelObject) == true;
        if (resultExists && impossibleTravelObject is not null) {
            return (SignInGuardResult)impossibleTravelObject;
        }
        SignInGuardResult result;
        if (ImpossibleTravelDetector is not null) {
            var isImpossibleTravel = await ImpossibleTravelDetector.IsImpossibleTravelLogin(httpContext, user);
            if (isImpossibleTravel) {
                result = SignInGuardResult.Failed(SignInWarning.ImpossibleTravel);
                httpContext?.Items.Add(HTTP_CONTEXT_ITEM_KEY, result);
                return result;
            }
        }
        result = SignInGuardResult.Success();
        httpContext?.Items.Add(HTTP_CONTEXT_ITEM_KEY, result);
        return result;
    }
}
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Server;

/// <summary>Default implementation of <see cref="ISignInGuard{TUser}"/> interface.</summary>
public class SignInGuard<TUser> : ISignInGuard<TUser> where TUser : User
{
    private static readonly string HTTP_CONTEXT_ITEM_KEY = $"{nameof(SignInGuard<TUser>)}:Result";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="SignInGuard{TUser}"/> class.</summary>
    /// <param name="impossibleTravelDetector">A service that detects whether a login attempt is made from an impossible location.</param>
    /// <param name="httpContextAccessor"></param>
    public SignInGuard(
        IHttpContextAccessor httpContextAccessor,
        IImpossibleTravelDetector<TUser>? impossibleTravelDetector = null
    ) {
        _httpContextAccessor = httpContextAccessor;
        ImpossibleTravelDetector = impossibleTravelDetector;
    }

    /// <inheritdoc />
    public IImpossibleTravelDetector<TUser>? ImpossibleTravelDetector { get; init; }

    /// <inheritdoc />
    public async Task<SignInGuardResult> IsSuspiciousLogin(TUser user) {
        object? impossibleTravelObject = null;
        var resultExists = _httpContextAccessor?.HttpContext?.Items.TryGetValue(HTTP_CONTEXT_ITEM_KEY, out impossibleTravelObject) == true;
        if (resultExists && impossibleTravelObject is not null) {
            return (SignInGuardResult)impossibleTravelObject;
        }
        SignInGuardResult? result;
        if (ImpossibleTravelDetector is not null) {
            var isImpossibleTravel = await ImpossibleTravelDetector.IsImpossibleTravelLogin(user);
            if (isImpossibleTravel) {
                result = SignInGuardResult.Failed(SignInWarning.ImpossibleTravel);
                _httpContextAccessor?.HttpContext?.Items.Add(HTTP_CONTEXT_ITEM_KEY, result);
                return result;
            }
        }
        result = SignInGuardResult.Success();
        _httpContextAccessor?.HttpContext?.Items.Add(HTTP_CONTEXT_ITEM_KEY, result);
        return result;
    }
}
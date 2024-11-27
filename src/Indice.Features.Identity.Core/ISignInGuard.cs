using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>Abstracts the process of running various rules that determine whether a login attempt is suspicious or not.</summary>
public interface ISignInGuard<TUser> where TUser : User
{
    /// <summary>A service that detects whether a login attempt is made from an impossible location.</summary>
    public IImpossibleTravelDetector<TUser>? ImpossibleTravelDetector { get; init; }
    /// <summary>Runs various rules and determines whether a login attempt is considered suspicious or not.</summary>
    /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    /// <param name="user">The current user.</param>
    Task<SignInGuardResult> IsSuspiciousLogin(HttpContext? httpContext, TUser user);
}

/// <summary>Implementation of <see cref="ISignInGuard{TUser}"/> where no check is made.</summary>
/// <typeparam name="TUser"></typeparam>
public class SignInGuardNoOp<TUser> : ISignInGuard<TUser> where TUser : User
{
    /// <inheritdoc />
    public IImpossibleTravelDetector<TUser>? ImpossibleTravelDetector { get; init; } = null;

    /// <inheritdoc />
    public Task<SignInGuardResult> IsSuspiciousLogin(HttpContext? httpContext, TUser user) => Task.FromResult(SignInGuardResult.Success());
}

/// <summary></summary>
public class SignInGuardResult
{
    /// <summary>Describes whether the result was successful.</summary>
    public bool Succeeded { get; private set; }
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; private set; }

    /// <summary>Creates a new instance of a successful <see cref="SignInGuardResult"/>.</summary>
    public static SignInGuardResult Success() => new() {
        Succeeded = true
    };

    /// <summary>Creates a new instance of a failed <see cref="SignInGuardResult"/>.</summary>
    /// <param name="warning"></param>
    public static SignInGuardResult Failed(SignInWarning warning) => new() {
        Succeeded = false,
        Warning = warning
    };
}

/// <summary>Describes a warning that may occur during a sign in event.</summary>
public enum SignInWarning
{
    /// <summary>Impossible travel detected.</summary>
    ImpossibleTravel
}

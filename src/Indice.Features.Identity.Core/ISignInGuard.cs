using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core;

/// <summary>Abstracts the process of running various rules that determine whether a login attempt is suspicious or not.</summary>
public interface ISignInGuard<TUser> where TUser : User
{
    /// <summary>Runs various rules and determines whether a login attempt is considered suspicious or not.</summary>
    /// <param name="user">The current user.</param>
    Task<bool> IsSuspiciousLogin(TUser user);
}

/// <summary>Implementation of <see cref="ISignInGuard{TUser}"/> where no check is made.</summary>
/// <typeparam name="TUser"></typeparam>
public class SignInGuardNoOp<TUser> : ISignInGuard<TUser> where TUser : User
{
    /// <inheritdoc />
    public Task<bool> IsSuspiciousLogin(TUser user) => Task.FromResult(false);
}

using System.Security.Claims;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Data.Stores;

/// <summary>Custom <see cref="IUserStore{T}"/> that provides password history features.</summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IExtendedUserStore<TUser> where TUser : User
{
    /// <summary>
    /// The password history limit is an integer indicating the number of passwords to keep track. 
    /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
    /// </summary>
    int? PasswordHistoryLimit { get; }
    /// <summary>
    /// The password expiration policy is the default setting that every new user created by the <see cref="UserManager{TUser}"/> will inherit in regards
    /// to when their password will need to be changed. This settings is only for new users created any only if no explicit password policy is set.
    /// </summary>
    PasswordExpirationPolicy? PasswordExpirationPolicy { get; }
    /// <summary>Sets the password expiration policy for the specified user.</summary>
    /// <param name="user">The user whose password expiration policy to set.</param>
    /// <param name="policy">The password expiration policy to set.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken);
    /// <summary>Sets the <see cref="User.PasswordExpired"/> property of the user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="expired">The value to use.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    Task SetPasswordExpiredAsync(TUser user, bool expired, CancellationToken cancellationToken);
    /// <summary>Sets the <see cref="User.LastSignInDate"/> property of the user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="timestamp">The <see cref="DateTimeOffset"/> value that the user signed in. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp, CancellationToken cancellationToken);
    /// <summary>Removes all claim instances of the specified type from the specified user</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="claimType">The claim type to be removed</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>An <see cref="IdentityResult"/></returns>
    Task<IdentityResult> RemoveAllClaimsAsync(TUser user, string claimType, CancellationToken cancellationToken = default);
    /// <summary>Find all user cliams of the specified type</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="claimType">The claim type to be removed</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A list of <see cref="IdentityUserClaim{TKey}"/></returns>
    Task<IList<Claim>> FindClaimsByTypeAsync(TUser user, string claimType, CancellationToken cancellationToken = default);
    /// <summary>Replaces any claims with the same claim type on the specified user with the newClaim.</summary>
    /// <param name="user">The user to replace the claim on.</param>
    /// <param name="claimType">The claim type to replace.</param>
    /// <param name="claimValue">The claim value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
    /// <remarks>This overload will purge any other instances of the claim type so that the added claim is has a single instance in the claims list. For example the 'given_name' claim that is commonly used as the FirstName</remarks>
    Task<IdentityResult> ReplaceClaimAsync(TUser user, string claimType, string? claimValue, CancellationToken cancellationToken = default);
}

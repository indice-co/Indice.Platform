using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core
{
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
        /// <summary>
        /// Some applications treat email (which is most cases is used only for communication purposes) as username. When this identity option is set to true,
        /// every operation that is applied to the email is also applied in the username. For example changing the email will also change the username.
        /// </summary>
        bool? EmailAsUserName { get; }
        /// <summary>Sets the password expiration policy for the specified user.</summary>
        /// <param name="user">The user whose password expiration policy to set.</param>
        /// <param name="policy">The password expiration policy to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken);
        /// <summary>Sets the <see cref="User.PasswordExpired"/> property of the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="changePassword">The value to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        Task SetPasswordExpiredAsync(TUser user, bool changePassword, CancellationToken cancellationToken);
        /// <summary>Sets the <see cref="User.LastSignInDate"/> property of the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="timestamp">The <see cref="DateTimeOffset"/> value that the user signed in. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp, CancellationToken cancellationToken);
    }
}

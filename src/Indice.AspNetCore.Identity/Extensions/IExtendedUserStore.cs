using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Extensions
{
    /// <summary>
    /// Custom <see cref="UserStore"/> that provides password history features.
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public interface IExtendedUserStore<TUser> where TUser : User
    {
        /// <summary>
        /// The password history limit is an integer indicating the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
        /// </summary>
        int? PasswordHistoryLimit { get; }
        /// <summary>
        /// The password history retention is a double indicating the number of days the each password stored into history will be retained.
        /// The expiration day is calculated according to the date changed and not the created date.
        /// </summary>
        double? PasswordHistoryRetentionDays { get; }
        /// <summary>
        /// The password expiration policy is the default setting that every new user created by the <see cref="UserManager{TUser}"/> will inherit in regards
        /// to when their password will need to be changed. This settings is only for new users created any only if no explicit password policy is set.
        /// </summary>
        PasswordExpirationPolicy? PasswordExpirationPolicy { get; }
        /// <summary>
        /// Sets the password expiration policy for the specified user.
        /// </summary>
        /// <param name="user">The user whose password expiration policy to set.</param>
        /// <param name="policy">The password expiration policy to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken);
        /// <summary>
        /// Sets the <see cref="User.PasswordExpired"/> property of the user.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="changePassword">The value to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        Task SetPasswordExpiredAsync(TUser user, bool changePassword, CancellationToken cancellationToken);
    }
}

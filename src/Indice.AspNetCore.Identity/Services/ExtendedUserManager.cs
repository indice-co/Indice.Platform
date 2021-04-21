using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Entities;
using Indice.AspNetCore.Identity.EntityFrameworkCore;
using Indice.AspNetCore.Identity.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Provides the APIs for managing user in a persistence store.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class ExtendedUserManager<TUser> : UserManager<TUser> where TUser : User
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedUserManager{TUser}"/>.
        /// </summary>
        /// <param name="userStore">The persistence store the manager will operate over.</param>
        /// <param name="identityMessageDescriber">Provides an extensibility point for localizing messages used inside the package.</param>
        /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="passwordHasher">The password hashing implementation to use when saving passwords.</param>
        /// <param name="userValidators">A collection of <see cref="IUserValidator{TUser}"/> to validate users against.</param>
        /// <param name="passwordValidators">A collection of <see cref="IPasswordValidator{TUser}"/> to validate passwords against.</param>
        /// <param name="keyNormalizer">The <see cref="ILookupNormalizer"/> to use when generating index keys for users.</param>
        /// <param name="errors">The <see cref="IdentityErrorDescriber"/> used to provider error messages.</param>
        /// <param name="services">The <see cref="IServiceProvider"/> used to resolve services.</param>
        /// <param name="logger">The logger used to log messages, warnings and errors.</param>
        public ExtendedUserManager(
            IdentityErrorDescriber errors,
            IdentityMessageDescriber identityMessageDescriber,
            IEnumerable<IPasswordValidator<TUser>> passwordValidators,
            IEnumerable<IUserValidator<TUser>> userValidators,
            ILogger<ExtendedUserManager<TUser>> logger,
            ILookupNormalizer keyNormalizer,
            IOptionsSnapshot<IdentityOptions> optionsAccessor,
            IPasswordHasher<TUser> passwordHasher,
            IServiceProvider services,
            IUserStore<TUser> userStore
        ) : base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
            MessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports usernames that are the same as emails
        /// </summary>
        public bool SupportsEmailAsUserName => GetUserStore()?.EmailAsUserName == true;

        /// <summary>
        /// Provides an extensibility point for localizing messages used inside the package.
        /// </summary>
        public IdentityMessageDescriber MessageDescriber { get; set; }

        /// <summary>
        /// Sets the password expiration policy for the specified user.
        /// </summary>
        /// <param name="user">The user whose password expiration policy to set.</param>
        /// <param name="policy">The password expiration policy to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task{IdentityResult}"/> that represents the asynchronous operation.</returns>
        public async Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var userStore = GetUserStore();
            await userStore.SetPasswordExpirationPolicyAsync(user, policy, cancellationToken);
            await base.UpdateAsync(user);
        }

        /// <summary>
        /// Sets the <see cref="User.PasswordExpired"/> property of the user.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="changePassword">The value to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        public async Task SetPasswordExpiredAsync(TUser user, bool changePassword, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var userStore = GetUserStore();
            await userStore.SetPasswordExpiredAsync(user, changePassword, cancellationToken);
            await base.UpdateAsync(user);
        }

        /// <summary>
        /// Reset's a user's password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="validatePassword">Whether to validate the password.</param>
        /// <returns>Whether the password has was successfully updated.</returns>
        /// <remarks>This overload is used for admin reset password. Bypasses token requirement of default <see cref="UserManager{TUser}.ResetPasswordAsync(TUser, string, string)"/></remarks>.
        public async Task<IdentityResult> ResetPasswordAsync(TUser user, string newPassword, bool validatePassword = true) {
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            var result = await base.UpdatePasswordHash(user, newPassword, validatePassword);
            if (!result.Succeeded) {
                return result;
            }
            await SetLockoutEndDateAsync(user, null);
            return await UpdateUserAsync(user);
        }

        /// <inheritdoc />
        public override async Task<IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword) {
            var result = await base.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) {
                return result;
            }
            if (await IsLockedOutAsync(user)) {
                await SetLockoutEndDateAsync(user, null);
                return await UpdateUserAsync(user);
            }
            return result;
        }

        private IExtendedUserStore<TUser> GetUserStore(bool throwOnFail = true) {
            var cast = Store as IExtendedUserStore<TUser>;
            if (throwOnFail && cast == null) {
                throw new NotSupportedException($"Store is not of type {nameof(ExtendedUserStore)}.");
            }
            return cast;
        }
    }
}

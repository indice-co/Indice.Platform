﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Events;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Provides the APIs for managing user in a persistence store.</summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class ExtendedUserManager<TUser> : UserManager<TUser> where TUser : User
    {
        private readonly IPlatformEventService _eventService;

        /// <summary>Creates a new instance of <see cref="ExtendedUserManager{TUser}"/>.</summary>
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
        /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
        public ExtendedUserManager(
            IUserStore<TUser> userStore,
            IOptionsSnapshot<IdentityOptions> optionsAccessor,
            IPasswordHasher<TUser> passwordHasher,
            IEnumerable<IUserValidator<TUser>> userValidators,
            IEnumerable<IPasswordValidator<TUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<ExtendedUserManager<TUser>> logger,
            IdentityMessageDescriber identityMessageDescriber,
            IPlatformEventService eventService
        ) : base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
            MessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        /// <summary>Gets a flag indicating whether the backing user store supports user name that are the same as emails.</summary>
        public bool SupportsEmailAsUserName => GetUserStore()?.EmailAsUserName == true;
        /// <summary>Provides an extensibility point for localizing messages used inside the package.</summary>
        public IdentityMessageDescriber MessageDescriber { get; }
        /// <summary>Returns an <see cref="IQueryable{Device}"/> collection of devices.</summary>
        public IQueryable<UserDevice> UserDevices => GetDeviceStore()?.UserDevices;


        /// <summary>Sets the password expiration policy for the specified user.</summary>
        /// <param name="user">The user whose password expiration policy to set.</param>
        /// <param name="policy">The password expiration policy to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task{IdentityResult}"/> that represents the asynchronous operation.</returns>
        public async Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var userStore = GetUserStore();
            await userStore.SetPasswordExpirationPolicyAsync(user, policy, cancellationToken);
            await UpdateAsync(user);
        }

        /// <summary>Sets the <see cref="User.PasswordExpired"/> property of the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="changePassword">The value to use.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        public async Task SetPasswordExpiredAsync(TUser user, bool changePassword, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var userStore = GetUserStore();
            await userStore.SetPasswordExpiredAsync(user, changePassword, cancellationToken);
            await UpdateAsync(user);
        }

        /// <summary>Sets the <see cref="User.LastSignInDate"/> property of the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="timestamp">The <see cref="DateTimeOffset"/> value that the user signed in. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
        /// <returns>The <see cref="Task{IdentityResult}"/> that represents the asynchronous operation.</returns>
        public async Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var userStore = GetUserStore();
            await userStore.SetLastSignInDateAsync(user, timestamp, cancellationToken);
            await UpdateAsync(user);
        }

        /// <summary>Creates the specified <paramref name="user"/> in the backing store with given password, as an asynchronous operation.</summary>
        /// <param name="user">The user to create.</param>
        /// <param name="password">The password for the user to hash and store.</param>
        /// <param name="validatePassword">Whether to validate the password.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        /// <remarks>This overload is used for admin reset password. Bypasses token requirement of default <see cref="UserManager{TUser}.ResetPasswordAsync(TUser, string, string)"/></remarks>
        public async Task<IdentityResult> CreateAsync(TUser user, string password, bool validatePassword) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (password is null) {
                throw new ArgumentNullException(nameof(password));
            }
            var result = await UpdatePasswordHash(user, password, validatePassword);
            if (!result.Succeeded) {
                return result;
            }
            return await CreateAsync(user);
        }

        /// <inheritdoc />
        public async override Task<IdentityResult> CreateAsync(TUser user) {
            var result = await base.CreateAsync(user);
            if (result.Succeeded) {
                await _eventService.Publish(new UserCreatedEvent(user));
            }
            return result;
        }

        /// <inheritdoc />
        public async override Task<IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword) {
            var result = await base.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded) {
                await _eventService.Publish(new PasswordChangedEvent(user));
            }
            return result;
        }

        /// <summary>Reset's a user's password.</summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="validatePassword">Whether to validate the password.</param>
        /// <returns>Whether the password has was successfully updated.</returns>
        /// <remarks>This overload is used for admin reset password. Bypasses token requirement of default <see cref="UserManager{TUser}.ResetPasswordAsync(TUser, string, string)"/></remarks>
        public async Task<IdentityResult> ResetPasswordAsync(TUser user, string newPassword, bool validatePassword = true) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var result = await base.UpdatePasswordHash(user, newPassword, validatePassword);
            if (!result.Succeeded) {
                return result;
            }
            await _eventService.Publish(new PasswordChangedEvent(user));
            result = await SetLockoutEndDateAsync(user, null);
            return result;
        }

        /// <inheritdoc />
        public override async Task<IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword) {
            var result = await base.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) {
                return result;
            }
            await _eventService.Publish(new PasswordChangedEvent(user));
            if (await IsLockedOutAsync(user)) {
                return await SetLockoutEndDateAsync(user, null);
            }
            return result;
        }

        /// <summary>Adds the developer-totp claim to the provided user and provides a random 6-digit code. If the user is not a member of the 'Developer' role, it is also added automatically.</summary>
        /// <param name="user">The user.</param>
        public async Task<IdentityResult> SetDeveloperTotpAsync(TUser user) {
            var isDeveloper = await IsInRoleAsync(user, BasicRoleNames.Developer);
            if (!isDeveloper) {
                await AddToRoleAsync(user, BasicRoleNames.Developer);
            }
            user.GenerateDeveloperTotp();
            return await UpdateUserAsync(user);
        }

        /// <inheritdoc />
        public override async Task<IdentityResult> AccessFailedAsync(TUser user) {
            var result = await base.AccessFailedAsync(user);
            if (await IsLockedOutAsync(user)) {
                await _eventService.Publish(new AccountLockedEvent(user));
            }
            return result;
        }

        /// <summary>Replaces any claims with the same claim type on the specified user with the newClaim.</summary>
        /// <param name="user">The user to replace the claim on.</param>
        /// <param name="claimType">The claim type to replace.</param>
        /// <param name="claimValue"></param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        /// <remarks>This overload will purge any other instances of the claim type so that the added claim is has a single instance in the claims list. For example the 'given_name' claim that is commonly used as the FirstName</remarks>
        public async Task<IdentityResult> ReplaceClaimAsync(TUser user, string claimType, string claimValue) {
            IdentityResult result;
            var allClaims = await base.GetClaimsAsync(user);
            var toReplace = allClaims.Where(x => x.Type == claimType).ToList();
            var newClaim = new Claim(claimType, claimValue);
            if (string.IsNullOrWhiteSpace(claimValue)) {
                return await base.RemoveClaimsAsync(user, toReplace);
            }
            if (toReplace.Any()) {
                if (toReplace.Count == 1) {
                    result = await base.ReplaceClaimAsync(user, toReplace.First(), newClaim);
                } else {
                    result = await base.RemoveClaimsAsync(user, toReplace);
                    if (!result.Succeeded) {
                        return result;
                    }
                    result = await base.AddClaimAsync(user, newClaim);
                }
            } else {
                result = await base.AddClaimAsync(user, newClaim);
            }
            return result;
        }

        /// <inheritdoc />
        public async override Task<IdentityResult> SetUserNameAsync(TUser user, string userName) {
            var result = await base.SetUserNameAsync(user, userName);
            if (result.Succeeded) {
                await _eventService.Publish(new UserNameChangedEvent(user));
            }
            return result;
        }

        /// <inheritdoc />
        public override async Task<IdentityResult> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token) {
            var result = await base.ChangePhoneNumberAsync(user, phoneNumber, token);
            if (result.Succeeded) {
                await _eventService.Publish(new PhoneNumberConfirmedEvent(user));
            }
            return result;
        }

        /// <inheritdoc />
        public async override Task<IdentityResult> ConfirmEmailAsync(TUser user, string token) {
            var result = await base.ConfirmEmailAsync(user, token);
            if (result.Succeeded) {
                await _eventService.Publish(new EmailConfirmedEvent(user));
            }
            return result;
        }

        /// <summary>Adds a new device to the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="device"/> parameters are null.</exception>
        public async Task<IdentityResult> CreateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(device));
            }
            var deviceStore = GetDeviceStore();
            var result = await deviceStore.CreateDeviceAsync(user, device, cancellationToken);
            if (result.Succeeded) {
                await _eventService.Publish(new DeviceCreatedEvent(device, user));
            }
            return result;
        }

        /// <summary>Updates the given device. If the device does not exists, it is automatically created.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to update (or create).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="device"/> parameters are null.</exception>
        public async Task<IdentityResult> UpdateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(device));
            }
            var deviceStore = GetDeviceStore();
            var result = await deviceStore.UpdateDeviceAsync(user, device, cancellationToken);
            if (result.Succeeded) {
                await _eventService.Publish(new DeviceUpdatedEvent(device, user));
            }
            return result;
        }

        /// <summary>Get the devices registered by the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> parameter is null.</exception>
        public Task<IList<UserDevice>> GetDevicesAsync(TUser user, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            return deviceStore.GetDevicesAsync(user, cancellationToken);
        }

        /// <summary>Sets the maximum number of devices a user can register.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="maxDevicesCount">The number of devices.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> parameter is null.</exception>
        public async Task<IdentityResult> SetMaxDevicesCountAsync(TUser user, int maxDevicesCount, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            var result = await deviceStore.SetMaxDevicesCountAsync(user, maxDevicesCount, cancellationToken);
            if (!result.Succeeded) {
                return result;
            }
            return await UpdateAsync(user);
        }

        /// <summary>Get the devices registered by the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="deviceId">The id of the device to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="deviceId"/> parameters are null.</exception>
        public Task<UserDevice> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            var deviceStore = GetDeviceStore();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            return deviceStore.GetDeviceByIdAsync(user, deviceId, cancellationToken);
        }

        /// <summary>Permanently deletes the specified device from the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task RemoveDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            await deviceStore.RemoveDeviceAsync(user, device, cancellationToken);
            await _eventService.Publish(new DeviceDeletedEvent(device, user));
        }

        /// <summary>Sets the device state to require username/password in the next login.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to update.</param>
        /// <param name="requiresPassword">Boolean value for <see cref="UserDevice.RequiresPassword"/> field.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task<IdentityResult> SetDeviceRequiresPasswordAsync(TUser user, UserDevice device, bool requiresPassword, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            return deviceStore.SetDeviceRequiresPasswordAsync(user, device, requiresPassword, cancellationToken);
        }

        /// <summary>Sets all user devices state to require username/password in the next login.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="requiresPassword">Boolean value for <see cref="UserDevice.RequiresPassword"/> field.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Task<IdentityResult> SetAllDevicesRequirePasswordAsync(TUser user, bool requiresPassword, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            return deviceStore.SetAllDevicesRequirePasswordAsync(user, requiresPassword, cancellationToken);
        }

        private IExtendedUserStore<TUser> GetUserStore(bool throwOnFail = true) {
            var cast = Store as IExtendedUserStore<TUser>;
            if (throwOnFail && cast is null) {
                throw new NotSupportedException($"Store does not implement {nameof(IExtendedUserStore<TUser>)}.");
            }
            return cast;
        }

        private IUserDeviceStore<TUser> GetDeviceStore(bool throwOnFail = true) {
            var cast = Store as IUserDeviceStore<TUser>;
            if (throwOnFail && cast is null) {
                throw new NotSupportedException($"Store does not implement {nameof(IUserDeviceStore<TUser>)}.");
            }
            return cast;
        }
    }
}

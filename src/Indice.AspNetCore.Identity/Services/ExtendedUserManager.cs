using System;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
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
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
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
            IPlatformEventService eventService,
            IConfiguration configuration
        ) : base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger) {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            MessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
            DefaultAllowedRegisteredDevices = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}:Devices").GetValue<int?>(nameof(DefaultAllowedRegisteredDevices)) ??
                                              configuration.GetSection($"{nameof(UserOptions)}:Devices").GetValue<int?>(nameof(DefaultAllowedRegisteredDevices));
            MaxAllowedRegisteredDevices = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}:Devices").GetValue<int?>(nameof(MaxAllowedRegisteredDevices)) ??
                                          configuration.GetSection($"{nameof(UserOptions)}:Devices").GetValue<int?>(nameof(MaxAllowedRegisteredDevices));
            // Validate settings where needed.
            if (DefaultAllowedRegisteredDevices.HasValue && MaxAllowedRegisteredDevices.HasValue && DefaultAllowedRegisteredDevices.Value > MaxAllowedRegisteredDevices.Value) {
                throw new ApplicationException("Value of setting DefaultAllowedRegisteredDevices cannot exceed the value of MaxAllowedRegisteredDevices.");
            }
            MaxTrustedDevices = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}:Devices").GetValue<int?>(nameof(MaxTrustedDevices)) ??
                                configuration.GetSection($"{nameof(UserOptions)}:Devices").GetValue<int?>(nameof(MaxTrustedDevices));
            TrustActivationDelay = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}:Devices").GetValue<TimeSpan?>(nameof(TrustActivationDelay)) ??
                                   configuration.GetSection($"{nameof(UserOptions)}:Devices").GetValue<TimeSpan?>(nameof(TrustActivationDelay));
            EnforceMfa = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.SignIn)}").GetValue<bool?>(nameof(EnforceMfa)) == true ||
                         configuration.GetSection(nameof(SignInOptions)).GetValue<bool?>(nameof(EnforceMfa)) == true;
        }

        /// <summary>Gets a flag indicating whether the backing user store supports user name that are the same as emails.</summary>
        public bool SupportsEmailAsUserName => GetUserStore()?.EmailAsUserName == true;
        /// <summary>Returns an <see cref="IQueryable{Device}"/> collection of devices.</summary>
        public IQueryable<UserDevice> UserDevices => GetDeviceStore()?.UserDevices;
        /// <summary></summary>
        public int? MaxTrustedDevices { get; }
        /// <summary></summary>
        public TimeSpan? TrustActivationDelay { get; }
        /// <summary>Provides an extensibility point for localizing messages used inside the package.</summary>
        public IdentityMessageDescriber MessageDescriber { get; }
        /// <summary>The maximum number of devices a user can register.</summary>
        public int? MaxAllowedRegisteredDevices { get; }
        /// <summary>The default number of devices a user can register.</summary>
        public int? DefaultAllowedRegisteredDevices { get; }
        /// <summary>Enforces multi factor authentication for all users.</summary>
        public bool EnforceMfa { get; }

        #region Method Overrides
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

        /// <inheritdoc />
        public override async Task<IdentityResult> AccessFailedAsync(TUser user) {
            var result = await base.AccessFailedAsync(user);
            if (await IsLockedOutAsync(user)) {
                await _eventService.Publish(new AccountLockedEvent(user));
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
            var isTwoFactorEnabled = await GetTwoFactorEnabledAsync(user);
            if (EnforceMfa && !isTwoFactorEnabled) {
                result = await SetTwoFactorEnabledAsync(user, true);
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
        #endregion

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
        /// <remarks>This overload is used for administrator reset password. Bypasses token requirement of default <see cref="UserManager{TUser}.ResetPasswordAsync(TUser, string, string)"/></remarks>
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

        /// <summary>Reset's a user's password.</summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="validatePassword">Whether to validate the password.</param>
        /// <returns>Whether the password has was successfully updated.</returns>
        /// <remarks>This overload is used for administrator reset password. Bypasses token requirement of default <see cref="UserManager{TUser}.ResetPasswordAsync(TUser, string, string)"/></remarks>
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

        /// <summary>Adds the <see cref="BasicClaimTypes.DeveloperTotp"/> claim to the provided user and provides a random 6-digit code. If the user is not a member of the 'Developer' role, it is also added automatically.</summary>
        /// <param name="user">The user.</param>
        public async Task<IdentityResult> SetDeveloperTotpAsync(TUser user) {
            var isDeveloper = await IsInRoleAsync(user, BasicRoleNames.Developer);
            if (!isDeveloper) {
                await AddToRoleAsync(user, BasicRoleNames.Developer);
            }
            user.GenerateDeveloperTotp();
            return await UpdateUserAsync(user);
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
            var userClaims = await GetClaimsAsync(user);
            var maxDevicesCountClaim = userClaims.FirstOrDefault(x => x.Type == BasicClaimTypes.MaxDevicesCount)?.Value;
            int? userMaxDevicesCount = null;
            if (maxDevicesCountClaim is not null && int.TryParse(maxDevicesCountClaim, out var parsedUserMaxDevicesClaim)) {
                userMaxDevicesCount = parsedUserMaxDevicesClaim;
            }
            var maxDevicesCount = userMaxDevicesCount ?? DefaultAllowedRegisteredDevices ?? int.MaxValue;
            var numberOfUserDevices = await deviceStore.GetDevicesCountAsync(user, cancellationToken);
            if (maxDevicesCount == numberOfUserDevices) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.MaxNumberOfDevices),
                    Description = MessageDescriber.MaxNumberOfDevices()
                });
            }
            device.UserId = user.Id;
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

        /// <summary>Get the devices registered by the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> parameter is null.</exception>
        public Task<IList<UserDevice>> GetTrustedDevicesAsync(TUser user, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            return deviceStore.GetTrustedDevicesAsync(user, cancellationToken);
        }

        /// <summary>Get the number of trusted devices registered by the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> parameter is null.</exception>
        public Task<int> GetTrustedDevicesCountAsync(TUser user, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
            return deviceStore.GetTrustedDevicesCountAsync(user, cancellationToken);
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
            if (maxDevicesCount < 0) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.InsufficientNumberOfDevices),
                    Description = MessageDescriber.InsufficientNumberOfDevices()
                });
            }
            // Check if user tries to set the number of allowed devices to a value greater than the allowed one. 
            if (MaxAllowedRegisteredDevices.HasValue && maxDevicesCount > MaxAllowedRegisteredDevices.Value) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.LargeNumberOfDevices),
                    Description = MessageDescriber.LargeNumberOfDevices(maxDevicesCount, MaxAllowedRegisteredDevices.Value)
                });
            }
            var deviceStore = GetDeviceStore();
            var numberOfUserDevices = await deviceStore.GetDevicesCountAsync(user, cancellationToken);
            // User tries to set the number of allowed devices to a value lower than the current number.
            if (numberOfUserDevices > maxDevicesCount) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.LargeNumberOfUserDevices),
                    Description = MessageDescriber.LargeNumberOfUserDevices(numberOfUserDevices, maxDevicesCount)
                });
            }
            return await ReplaceClaimAsync(user, BasicClaimTypes.MaxDevicesCount, maxDevicesCount.ToString());
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
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceStore = GetDeviceStore();
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
        public async Task<IdentityResult> SetDeviceRequiresPasswordAsync(TUser user, UserDevice device, bool requiresPassword, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(user));
            }
            device.RequiresPassword = requiresPassword;
            return await UpdateDeviceAsync(user, device, cancellationToken);
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

        /// <summary>Begins the process of trusting a user device.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to mark as trusted.</param>
        /// <param name="swapDeviceId">The id of the device to remove before trusting the defined device.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<IdentityResult> SetTrustedDevice(TUser user, UserDevice device, string swapDeviceId = null, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(user));
            }
            // 1. Device is already trusted.
            if (device.IsTrusted) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.DeviceAlreadyTrusted),
                    Description = MessageDescriber.DeviceAlreadyTrusted()
                });
            }
            // 2. We need to check if device is pending trust activation. If so, trust activation date was set during a call prior to this.
            if (device.IsPendingTrustActivation) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(MessageDescriber.DevicePendingTrustActivation),
                    Description = MessageDescriber.DevicePendingTrustActivation()
                });
            }
            // 3. Check if a swap device id is specified. If this is the case, then we have to check if the specified device id for swap:
            // i)  belongs to the current user
            // ii) is already trusted
            var deviceStore = GetDeviceStore();
            if (!string.IsNullOrWhiteSpace(swapDeviceId)) {
                var swapDevice = await GetDeviceByIdAsync(user, swapDeviceId, cancellationToken);
                if (swapDevice is null || !swapDevice.IsTrusted) {
                    return IdentityResult.Failed(new IdentityError {
                        Code = nameof(UserDevice),
                        Description = "Device specified for swap is not valid."
                    });
                }
                var result = await SetUntrustedDevice(user, swapDevice, cancellationToken);
                if (!result.Succeeded) {
                    return result;
                }
            }
            // 4. At this point there are two cases:
            // a. The user wants to request device trust activation. If no delay is specified, we immediately trust the device, going to case b.
            var isDeviceActivationRequest = !device.TrustActivationDate.HasValue;
            if (isDeviceActivationRequest) {
                if (MaxTrustedDevices is > 0) {
                    var trustedOrPendingDevices = await deviceStore.GetTrustedOrPendingDevicesCountAsync(user, cancellationToken);
                    if (trustedOrPendingDevices >= MaxTrustedDevices.Value) {
                        return IdentityResult.Failed(new IdentityError {
                            Code = nameof(MessageDescriber.TrustedDevicesLimitReached),
                            Description = MessageDescriber.TrustedDevicesLimitReached()
                        });
                    }
                }
                device.TrustActivationDate = DateTimeOffset.UtcNow.Add(TrustActivationDelay ?? TimeSpan.Zero);
                await _eventService.Publish(new DeviceTrustRequestedEvent(device, user));
            }
            // b. The user waited for the required delay to pass and now wants to activate device trust.
            var isDeviceTrustRequest = device.TrustActivationDate.HasValue && !device.IsPendingTrustActivation;
            if (isDeviceTrustRequest) {
                device.IsTrusted = true;
            }
            // 5. Commit changes to the database.
            return await UpdateDeviceAsync(user, device, cancellationToken);
        }

        /// <summary>Sets a device as untrusted.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to mark as trusted.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        public async Task<IdentityResult> SetUntrustedDevice(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device.IsPendingTrustActivation) {
                return IdentityResult.Failed(new IdentityError {
                    Code = nameof(UserDevice.TrustActivationDate),
                    Description = MessageDescriber.DevicePendingTrustActivation()
                });
            }
            device.TrustActivationDate = null;
            device.IsTrusted = false;
            return await UpdateDeviceAsync(user, device, cancellationToken);
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

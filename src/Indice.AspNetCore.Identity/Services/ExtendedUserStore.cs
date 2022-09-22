using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity.Data
{
    /// <inheritdoc/>
    public class ExtendedUserStore : ExtendedUserStore<IdentityDbContext, User, Role>
    {
        /// <summary>Creates a new instance of <see cref="ExtendedUserStore"/>.</summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(IdentityDbContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, configuration, describer) { }
    }

    /// <inheritdoc/>
    public class ExtendedUserStore<TContext> : ExtendedUserStore<TContext, User, IdentityRole> where TContext : IdentityDbContext<User, IdentityRole>
    {
        /// <summary>Creates a new instance of <see cref="ExtendedUserStore"/>.</summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, configuration, describer) { }
    }

    /// <inheritdoc/>
    public class ExtendedUserStore<TContext, TUser, TRole> : UserStore<TUser, TRole, TContext>, IExtendedUserStore<TUser>, IUserDeviceStore<TUser>
        where TContext : IdentityDbContext<TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        /// <summary>Creates a new instance of <see cref="ExtendedUserStore{TContext, TUser, TRole}"/>.</summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, describer) {
            PasswordHistoryLimit = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(PasswordHistoryLimit)) ??
                                   configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
            PasswordExpirationPolicy = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<PasswordExpirationPolicy?>(nameof(PasswordExpirationPolicy)) ??
                                       configuration.GetSection(nameof(PasswordOptions)).GetValue<PasswordExpirationPolicy?>(nameof(PasswordExpirationPolicy));
            EmailAsUserName = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}").GetValue<bool?>(nameof(EmailAsUserName)) ??
                              configuration.GetSection(nameof(UserOptions)).GetValue<bool?>(nameof(EmailAsUserName));
            MaxDevicesCount = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}").GetValue<int?>(nameof(MaxDevicesCount)) ??
                              configuration.GetSection(nameof(UserOptions)).GetValue<int?>(nameof(MaxDevicesCount));
        }

        private DbSet<UserDevice> Devices => Context.Set<UserDevice>();
        /// <inheritdoc/>
        public IQueryable<UserDevice> UserDevices => Devices.AsQueryable();
        /// <inheritdoc/>
        public int? PasswordHistoryLimit { get; protected set; }
        /// <inheritdoc/>
        public PasswordExpirationPolicy? PasswordExpirationPolicy { get; protected set; }
        /// <inheritdoc/>
        public bool? EmailAsUserName { get; protected set; }
        /// <inheritdoc/>
        public int? MaxDevicesCount { get; protected set; }

        /// <inheritdoc/>
        public override async Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default) {
            var changeDate = DateTime.UtcNow;
            if (PasswordHistoryLimit.HasValue && !string.IsNullOrWhiteSpace(passwordHash)) {
                var numberOfPasswordsToKeep = Math.Max(PasswordHistoryLimit.Value, 0);
                var toPurge = await Context.Set<UserPassword>()
                                           .Where(x => x.UserId == user.Id)
                                           .OrderByDescending(x => x.DateCreated)
                                           .Skip(numberOfPasswordsToKeep)
                                           .ToArrayAsync();
                Context.Set<UserPassword>().RemoveRange(toPurge);
                await Context.Set<UserPassword>()
                             .AddAsync(new UserPassword {
                                 UserId = user.Id,
                                 DateCreated = changeDate,
                                 PasswordHash = passwordHash
                             });
            }
            user.LastPasswordChangeDate = changeDate;
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            await base.SetPasswordHashAsync(user, passwordHash, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            // Set the policy.
            user.PasswordExpirationPolicy = policy;
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetPasswordExpiredAsync(TUser user, bool changePassword, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            user.PasswordExpired = changePassword;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default) {
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            // If EmailAsUserName option is enabled and a username is not set, then assign email to username.
            if (EmailAsUserName.HasValue && EmailAsUserName.Value) {
                user.UserName = user.Email;
            }
            return base.UpdateAsync(user, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default) {
            user.CreateDate = DateTimeOffset.UtcNow;
            var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
            // If user does not already have a policy assigned use the default policy.
            // If the user does not have a password he is probably coming from an external provider, so no need to assign a password expiration policy.
            if (hasPassword) {
                if (!user.PasswordExpirationPolicy.HasValue) {
                    user.PasswordExpirationPolicy = PasswordExpirationPolicy;
                }
                user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            }
            // If EmailAsUserName option is enabled and a username is not set, then assign email to username.
            if (EmailAsUserName.HasValue && EmailAsUserName.Value && string.IsNullOrWhiteSpace(user.UserName)) {
                user.UserName = user.Email;
            }
            return base.CreateAsync(user, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default) {
            if (EmailAsUserName.HasValue && EmailAsUserName.Value) {
                await base.SetUserNameAsync(user, email, cancellationToken);
            }
            await base.SetEmailAsync(user, email, cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default) {
            await base.SetUserNameAsync(user, userName, cancellationToken);
            if (EmailAsUserName.HasValue && EmailAsUserName.Value) {
                await base.SetEmailAsync(user, userName, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            user.LastSignInDate = timestamp ?? DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> AddDeviceAsync(TUser user, Device device, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (device is null) {
                throw new ArgumentNullException(nameof(device));
            }
            var numberOfUserDevices = await Devices.CountAsync(x => x.UserId == user.Id);
            var maxDevicesCount = user.MaxDevicesCount ?? MaxDevicesCount ?? int.MaxValue;
            if (numberOfUserDevices == maxDevicesCount) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "MaxNumberOfDevices",
                    Description = "You have reached the maximum number of registered devices."
                });
            }
            Devices.Add(new UserDevice {
                Data = device.Data,
                DateCreated = device.DateCreated ?? DateTimeOffset.UtcNow,
                DeviceId = device.DeviceId,
                Model = device.Model,
                Name = device.Name,
                OsVersion = device.OsVersion,
                Platform = device.Platform,
                UserId = user.Id
            });
            await SaveChanges(cancellationToken);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IList<Device>> GetDevicesAsync(TUser user, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            return await Devices.Where(device => device.UserId == user.Id).Select(device => new Device {
                Data = device.Data,
                DateCreated = device.DateCreated,
                DeviceId = device.DeviceId,
                IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
                LastSignInDate = device.LastSignInDate,
                Model = device.Model,
                Name = device.Name,
                OsVersion = device.OsVersion,
                Platform = device.Platform
            })
            .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Device> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            var device = await Devices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, cancellationToken);
            if (device is not null) {
                return new Device {
                    Data = device.Data,
                    DateCreated = device.DateCreated,
                    DeviceId = device.DeviceId,
                    IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
                    LastSignInDate = device.LastSignInDate,
                    Model = device.Model,
                    Name = device.Name,
                    OsVersion = device.OsVersion,
                    Platform = device.Platform
                };
            }
            return default;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> UpdateDeviceAsync(TUser user, Device device, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var deviceId = device.DeviceId;
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "MissingDeviceId",
                    Description = "Device id is missing."
                });
            }
            var foundDevice = await Devices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, cancellationToken);
            if (foundDevice is not null) {
                foundDevice.Data = device.Data;
                foundDevice.IsPushNotificationsEnabled = foundDevice.IsPushNotificationsEnabled;
                foundDevice.Model = device.Model;
                foundDevice.Name = device.Name;
                foundDevice.OsVersion = device.OsVersion;
                foundDevice.Platform = device.Platform;
                await SaveChanges(cancellationToken);
                return IdentityResult.Success;
            }
            return await AddDeviceAsync(user, device, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> SetMaxDevicesCountAsync(TUser user, int maxDevicesCount, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            if (maxDevicesCount < 1) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "InsufficientNumberOfDevices",
                    Description = "User must have at least 1 device."
                });
            }
            if (MaxDevicesCount.HasValue && maxDevicesCount > MaxDevicesCount.Value) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "LargeNumberOfDevices",
                    Description = $"Cannot set max number to {maxDevicesCount}."
                });
            }
            var numberOfUserDevices = await Devices.CountAsync(x => x.UserId == user.Id);
            if (numberOfUserDevices > maxDevicesCount) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "LargeNumberOfDevices",
                    Description = $"User already has {numberOfUserDevices} devices. Cannot set max number to {maxDevicesCount}."
                });
            }
            user.MaxDevicesCount = maxDevicesCount;
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task RemoveDeviceAsync(TUser user, string deviceId, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user is null) {
                throw new ArgumentNullException(nameof(user));
            }
            var device = await Devices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, cancellationToken);
            if (device is not null) {
                Devices.Remove(device);
                await SaveChanges(cancellationToken);
            }
        }
    }
}

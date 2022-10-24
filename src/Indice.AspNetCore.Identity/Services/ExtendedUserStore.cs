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
        }

        private DbSet<UserDevice> UserDeviceSet => Context.Set<UserDevice>();
        /// <inheritdoc/>
        public IQueryable<UserDevice> UserDevices => UserDeviceSet.AsQueryable();
        /// <inheritdoc/>
        public int? PasswordHistoryLimit { get; protected set; }
        /// <inheritdoc/>
        public PasswordExpirationPolicy? PasswordExpirationPolicy { get; protected set; }
        /// <inheritdoc/>
        public bool? EmailAsUserName { get; protected set; }

        #region Method Overrides
        /// <inheritdoc/>
        public override async Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default) {
            var changeDate = DateTime.UtcNow;
            if (PasswordHistoryLimit.HasValue && !string.IsNullOrWhiteSpace(passwordHash)) {
                var numberOfPasswordsToKeep = Math.Max(PasswordHistoryLimit.Value, 0);
                var toPurge = await Context.Set<UserPassword>()
                                           .Where(x => x.UserId == user.Id)
                                           .OrderByDescending(x => x.DateCreated)
                                           .Skip(numberOfPasswordsToKeep)
                                           .ToArrayAsync(cancellationToken);
                Context.Set<UserPassword>().RemoveRange(toPurge);
                await Context.Set<UserPassword>()
                             .AddAsync(new UserPassword {
                                 UserId = user.Id,
                                 DateCreated = changeDate,
                                 PasswordHash = passwordHash
                             }, cancellationToken);
            }
            user.LastPasswordChangeDate = changeDate;
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            await base.SetPasswordHashAsync(user, passwordHash, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
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
        #endregion

        /// <inheritdoc/>
        public Task SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
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
            user.PasswordExpired = changePassword;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SetLastSignInDateAsync(TUser user, DateTimeOffset? timestamp, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            user.LastSignInDate = timestamp ?? DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> CreateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            UserDeviceSet.Add(device);
            try {
                await SaveChanges(cancellationToken);
            } catch (DbUpdateConcurrencyException) {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IList<UserDevice>> GetDevicesAsync(TUser user, GetDevicesFilter filter = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await UserDeviceSet
                .Include(device => device.User)
                .ApplyFilter(filter)
                .Where(device => device.UserId == user.Id)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetDevicesCountAsync(TUser user, GetDevicesFilter filter = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await UserDevices.ApplyFilter(filter).CountAsync(device => device.UserId == user.Id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<UserDevice> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await UserDeviceSet.Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> UpdateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            Context.Update(device);
            try {
                await SaveChanges(cancellationToken);
            } catch (DbUpdateConcurrencyException) {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task RemoveDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            UserDeviceSet.Remove(device);
            await SaveChanges(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> SetAllDevicesRequirePasswordAsync(TUser user, bool requiresPassword, CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var devices = await GetDevicesAsync(user, null, cancellationToken);
            foreach (var device in devices) {
                device.RequiresPassword = requiresPassword;
            }
            try {
                await SaveChanges(cancellationToken);
            } catch (DbUpdateConcurrencyException) {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityResult.Success;
        }
    }

    internal static class QueryableExtensions
    {
        public static IQueryable<UserDevice> ApplyFilter(this IQueryable<UserDevice> query, GetDevicesFilter filter) {
            if (filter is not null) {
                query = query.Where(device =>
                    (filter.IsTrusted == null || device.IsTrusted == filter.IsTrusted) ||
                    (filter.IsPendingTrustActivation == null || (device.TrustActivationDate.HasValue && device.TrustActivationDate.Value > DateTimeOffset.UtcNow))
                );
            }
            return query;
        }
    }
}

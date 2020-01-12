using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// Custom <see cref="UserStore"/> that provides password history features.
    /// </summary>
    public class ExtendedUserStore : ExtendedUserStore<IdentityDbContext, User, IdentityRole>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedUserStore"/>.
        /// </summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(IdentityDbContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, configuration, describer) { }
    }

    /// <summary>
    /// Custom <see cref="UserStore"/> that provides password history features.
    /// </summary>
    public class ExtendedUserStore<TContext> : ExtendedUserStore<TContext, User, IdentityRole> where TContext : IdentityDbContext<User, IdentityRole>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedUserStore"/>.
        /// </summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, configuration, describer) { }
    }

    /// <summary>
    /// Custom <see cref="UserStore"/> that provides password history features.
    /// </summary>
    public class ExtendedUserStore<TContext, TUser, TRole> : UserStore<TUser, TRole, TContext>
        where TContext : IdentityDbContext<TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedUserStore{TContext, TUser, TRole}"/>.
        /// </summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, describer) {
            PasswordHistoryLimit = configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
            PasswordExpirationPolicy = configuration.GetSection(nameof(PasswordOptions)).GetValue<PasswordExpirationPolicy?>(nameof(PasswordExpirationPolicy));
        }

        /// <summary>
        /// The password history limit is an integer indicating the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
        /// </summary>
        protected int? PasswordHistoryLimit { get; }

        /// <summary>
        /// The password history retention is an double indicating the number of days the each password stored into history will be retained.
        /// The expiration day is calculated according to the date changed and not the created date.
        /// </summary>
        protected double? PasswordHistoryRetentionDays { get; }

        /// <summary>
        /// The password expiration policy is the default setting that every new user created by the usermanager will inherit in regards
        /// to when their password will need to be changed. This settings is only for new users creted any only if no explicit password policy is set.
        /// </summary>
        protected PasswordExpirationPolicy? PasswordExpirationPolicy { get; }

        /// <summary>
        /// Sets the password hash for the specified user.
        /// </summary>
        /// <param name="user">The user whose password hash to set.</param>
        /// <param name="passwordHash">The password hash to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the
        /// operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken)) {
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

        /// <summary>
        /// Updates the specified user in the user store.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default) {
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            return base.UpdateAsync(user, cancellationToken);
        }


        /// <summary>
        /// Sets the password expiration policy for the specified user.
        /// </summary>
        /// <param name="user">The user whose password expiration policy to set.</param>
        /// <param name="policy">The password expiration policy to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task{IdentityResult}"/> that represents the asynchronous operation.</returns>
        public async Task<IdentityResult> SetPasswordExpirationPolicyAsync(TUser user, PasswordExpirationPolicy? policy, CancellationToken cancellationToken = default(CancellationToken)) {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }
            // Set the policy.
            user.PasswordExpirationPolicy = policy;
            // Calculate expiration date based on policy.
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            Context.Attach(user);
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            Context.Update(user);
            try {
                await SaveChanges(cancellationToken);
            } catch (DbUpdateConcurrencyException) {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Creates the specified user in the user store.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="IdentityResult"/></returns>
        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken)) {
            user.CreateDate = DateTimeOffset.UtcNow;
            // if user does not already have a policy assigned use the default policy.
            if (!user.PasswordExpirationPolicy.HasValue) { 
                user.PasswordExpirationPolicy = PasswordExpirationPolicy;
            }
            return base.CreateAsync(user, cancellationToken);
        }

    }
}

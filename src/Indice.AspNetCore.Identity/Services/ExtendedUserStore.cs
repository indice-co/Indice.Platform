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
    /// <typeparam name="TContext">The DbContext to use for the Identity framework.</typeparam>
    public class ExtendedUserStore<TContext> : ExtendedUserStore where TContext : IdentityDbContext
    {
        /// <summary>
        /// Constructs the user store.
        /// </summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(TContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, configuration, describer) { }
    }

    /// <summary>
    /// Custom <see cref="UserStore"/> that provides password history features.
    /// </summary>
    public class ExtendedUserStore : UserStore<User>
    {
        /// <summary>
        /// Constructs the user store.
        /// </summary>
        /// <param name="context">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="describer">Service to enable localization for application facing identity errors.</param>
        public ExtendedUserStore(IdentityDbContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, describer) {
            PasswordHistoryLimit = configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
        }

        /// <summary>
        /// The password history limit is an integer indicating the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
        /// </summary>
        protected int? PasswordHistoryLimit { get; }

        /// <summary>
        /// Sets the password hash for the specified user.
        /// </summary>
        /// <param name="user">The user whose password hash to set.</param>
        /// <param name="passwordHash">The password hash to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the
        /// operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken)) {
            if (PasswordHistoryLimit.HasValue && !string.IsNullOrWhiteSpace(passwordHash)) {
                var numberOfPasswordsToKeep = Math.Max(PasswordHistoryLimit.Value, 0);
                var toPurge = await Context.Set<UserPassword>().Where(x => x.UserId == user.Id).OrderByDescending(x => x.DateCreated).Skip(numberOfPasswordsToKeep).ToArrayAsync();
                Context.Set<UserPassword>().RemoveRange(toPurge);
                await Context.Set<UserPassword>().AddAsync(new UserPassword { UserId = user.Id, DateCreated = DateTime.UtcNow, PasswordHash = passwordHash });
            }

            user.LastPasswordChangeDate = DateTime.UtcNow;
            await base.SetPasswordHashAsync(user, passwordHash, cancellationToken);
        }

        /// <summary>
        /// Creates the specified user in the user store.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="IdentityResult"/></returns>
        public override Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken = default(CancellationToken)) {
            user.CreateDate = DateTimeOffset.UtcNow;
            return base.CreateAsync(user, cancellationToken);
        }

    }
}

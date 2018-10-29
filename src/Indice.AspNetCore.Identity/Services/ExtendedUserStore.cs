using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Custom UserStore that uses some additional dates relate
    /// </summary>
    public class ExtendedUserStore : UserStore<User>
    {
        /// <summary>
        /// Creates the user store
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <param name="describer"></param>
        public ExtendedUserStore(IdentityDbContext context, IConfiguration configuration, IdentityErrorDescriber describer = null) : base(context, describer) {
            PasswordHistoryLimit = configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
        }

        /// <summary>
        /// The password history limit is an integer indicating 
        /// the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches 
        /// any stored in the history table.
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
            user.LastPasswordChange = DateTime.UtcNow;
            await base.SetPasswordHashAsync(user, passwordHash, cancellationToken);
        }

        /// <summary>
        /// Creates the specified user in the user store.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the
        /// operation should be canceled.</param>
        /// <returns>The <see cref="IdentityResult"/></returns>
        public override Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken = default(CancellationToken)) {
            user.CreateDate = DateTimeOffset.UtcNow;
            return base.CreateAsync(user, cancellationToken);
        }
    }
}

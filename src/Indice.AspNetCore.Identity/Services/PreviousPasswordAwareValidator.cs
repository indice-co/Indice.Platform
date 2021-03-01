using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.EntityFrameworkCore;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// An <see cref="IPasswordValidator{TUser}" /> that checks a number of previous passwords for equality.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public class PreviousPasswordAwareValidator<TContext> : PreviousPasswordAwareValidator where TContext : IdentityDbContext
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
        public PreviousPasswordAwareValidator(TContext dbContext, IConfiguration configuration, IdentityMessageDescriber messageDescriber) : base(dbContext, configuration, messageDescriber) { }
    }

    /// <summary>
    /// An <see cref="IPasswordValidator{TUser}" /> that checks a number of previous passwords for equality.
    /// </summary>
    public class PreviousPasswordAwareValidator : PreviousPasswordAwareValidator<User, IdentityRole>
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
        public PreviousPasswordAwareValidator(IdentityDbContext<User, IdentityRole> dbContext, IConfiguration configuration, IdentityMessageDescriber messageDescriber) : base(dbContext, configuration, messageDescriber) { }
    }

    /// <summary>
    /// An <see cref="IPasswordValidator{TUser}" /> that checks a number of previous passwords for equality.
    /// </summary>
    public class PreviousPasswordAwareValidator<TUser, TRole> : PreviousPasswordAwareValidator<IdentityDbContext<TUser, TRole>, TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
        public PreviousPasswordAwareValidator(IdentityDbContext<TUser, TRole> dbContext, IConfiguration configuration, IdentityMessageDescriber messageDescriber) : base(dbContext, configuration, messageDescriber) { }
    }

    /// <summary>
    /// An <see cref="IPasswordValidator{TUser}" /> that checks a number of previous passwords for equality.
    /// </summary>
    /// <typeparam name="TContext">Type of <see cref="DbContext"/>.</typeparam>
    /// <typeparam name="TUser">Type of user.</typeparam>
    /// <typeparam name="TRole">Type of role.</typeparam>
    public class PreviousPasswordAwareValidator<TContext, TUser, TRole> : IPasswordValidator<TUser>
        where TContext : IdentityDbContext<TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        /// <summary>
        /// The code used when describing the <see cref="IdentityError"/>.
        /// </summary>
        public static string ErrorDescriber = "PasswordHistory";

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
        public PreviousPasswordAwareValidator(TContext dbContext, IConfiguration configuration, IdentityMessageDescriber messageDescriber) {
            DbContext = dbContext;
            MessageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
            PasswordHistoryLimit = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(PasswordHistoryLimit)) ??
                                   configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
        }

        /// <summary>
        /// The database context.
        /// </summary>
        public TContext DbContext { get; }
        /// <summary>
        /// The password history limit is an integer indicating the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
        /// </summary>
        public int? PasswordHistoryLimit { get; }
        /// <summary>
        /// Provides the various messages used throughout Indice packages.
        /// </summary>
        public IdentityMessageDescriber MessageDescriber { get; set; }

        /// <summary>
        /// Validates a password as an asynchronous operation.
        /// </summary>
        /// <param name="manager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="user">The user whose password should be validated.</param>
        /// <param name="password">The password supplied for validation</param>
        /// <returns></returns>
        public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
            var result = IdentityResult.Success;
            if (PasswordHistoryLimit > 0) {
                var usedPasswords = await DbContext.UserPasswordHistory
                                                   .Where(x => x.UserId == user.Id)
                                                   .OrderByDescending(x => x.DateCreated)
                                                   .Take(PasswordHistoryLimit.Value)
                                                   .Select(x => x.PasswordHash)
                                                   .ToListAsync();
                // We need to check the current password if the user has nothing in the password history table.
                // This essentially means the feature has recently been enabled and the user has not since changed his password.
                usedPasswords.Insert(0, user.PasswordHash);
                var isUsedBefore = usedPasswords.Count > 0 &&
                                   usedPasswords.Where(hash => !string.IsNullOrEmpty(hash))
                                                .Distinct()
                                                .Any(hash => manager.PasswordHasher.VerifyHashedPassword(user, hash, password) == PasswordVerificationResult.Success);
                if (isUsedBefore) {
                    result = IdentityResult.Failed(new IdentityError {
                        Code = ErrorDescriber,
                        Description = MessageDescriber.PasswordRecentlyUsed
                    });
                }
            }
            return result;
        }
    }
}

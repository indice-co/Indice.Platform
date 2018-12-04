using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity.Services
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
        public PreviousPasswordAwareValidator(TContext dbContext, IConfiguration configuration) : base(dbContext, configuration) { }
    }

    /// <summary>
    /// An <see cref="IPasswordValidator{TUser}" /> that checks a number of previous passwords for equality.
    /// </summary>
    public class PreviousPasswordAwareValidator : IPasswordValidator<User>
    {
        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for the Identity framework.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public PreviousPasswordAwareValidator(IdentityDbContext dbContext, IConfiguration configuration) {
            DbContext = dbContext;
            PasswordHistoryLimit = configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(PasswordHistoryLimit));
        }

        /// <summary>
        /// The database context.
        /// </summary>
        protected IdentityDbContext DbContext { get; }

        /// <summary>
        /// The password history limit is an integer indicating the number of passwords to keep track. 
        /// Then when a user changes his password these will be check against so that no new password matches any stored in the history table.
        /// </summary>
        protected int? PasswordHistoryLimit { get; }

        /// <summary>
        /// Validates a password as an asynchronous operation.
        /// </summary>
        /// <param name="manager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="user">The user whose password should be validated.</param>
        /// <param name="password">The password supplied for validation</param>
        /// <returns></returns>
        public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string password) {
            var result = IdentityResult.Success;

            if (PasswordHistoryLimit.HasValue) {
                var usedPasswords = await DbContext.UserPasswordHistory
                                                   .Where(x => x.UserId == user.Id)
                                                   .OrderByDescending(x => x.DateCreated).Take(PasswordHistoryLimit.Value)
                                                   .Select(x => x.PasswordHash).ToArrayAsync();

                if (usedPasswords.Length > 0 && usedPasswords.Where(hash => !string.IsNullOrEmpty(hash)).Any(hash => manager.PasswordHasher.VerifyHashedPassword(user, hash, password) == PasswordVerificationResult.Success)) {
                    result = IdentityResult.Failed(new IdentityError() { Code = "PasswordHistory", Description = "This password has been used recently." });
                }
            }

            return result;
        }
    }
}

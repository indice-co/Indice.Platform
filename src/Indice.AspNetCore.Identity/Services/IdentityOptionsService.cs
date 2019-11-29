using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// A service that retrieves the ASP.NET Identity options from database.
    /// </summary>
    internal class IdentityOptionsService<TContext, TUser, TRole>
        where TContext : IdentityDbContext<TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        private readonly TContext _dbContext;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContext"></param>
        public IdentityOptionsService(TContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Gets password options from database.
        /// </summary>
        public async Task<PasswordOptions> GetPasswordOptions() {
            var databaseExists = _dbContext.Database.GetService<IRelationalDatabaseCreator>().Exists();
            if (!databaseExists) {
                return new PasswordOptions();
            }
            var passwordOptions2 = await _dbContext.Set<SystemSettings>().SingleOrDefaultAsync(x => x.Id == "password-options");
            var passwordOptions = new PasswordOptions {
                RequiredLength = 2,
                RequiredUniqueChars = 0,
                RequireNonAlphanumeric = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireDigit = false
            };
            return await Task.FromResult(passwordOptions);
        }
    }
}

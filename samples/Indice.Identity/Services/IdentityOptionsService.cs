using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Indice.Identity.Services
{
    /// <summary>
    /// A service that retrieves the ASP.NET Identity options from database.
    /// </summary>
    public class IdentityOptionsService
    {
        /// <summary>
        /// Gets password options from database.
        /// </summary>
        public Task<PasswordOptions> GetPasswordOptions() {
            var passwordOptions = new PasswordOptions {
                RequiredLength = 2,
                RequiredUniqueChars = 0,
                RequireNonAlphanumeric = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireDigit = false
            };
            return Task.FromResult(passwordOptions);
        }
    }
}

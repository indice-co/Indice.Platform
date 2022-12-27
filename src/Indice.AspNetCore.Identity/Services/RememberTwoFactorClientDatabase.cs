using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary></summary>
    /// <typeparam name="TUser">The user entity.</typeparam>
    public class RememberTwoFactorClientDatabase<TUser> : IRememberTwoFactorClientProvider<TUser> where TUser : User
    {
        /// <inheritdoc />
        public Task RememberTwoFactorClientAsync(TUser user) {
            return Task.CompletedTask;
        }
    }
}

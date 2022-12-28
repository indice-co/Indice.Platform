using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Abstracts the way that a client (browser) is remembered across login operations, using the <see cref="ExtendedSignInManager{TUser}"/>.</summary>
    /// <typeparam name="TUser">The user entity.</typeparam>
    public interface IRememberTwoFactorClientProvider<TUser> where TUser : User
    {
        /// <summary>Returns a flag indicating if the current client browser has been remembered by two factor authentication for the user attempting to login, as an asynchronous operation.</summary>
        /// <param name="user">The user entity.</param>
        Task<bool> IsTwoFactorClientRememberedAsync(TUser user);
        /// <summary>Sets a flag on the browser or server to indicate the user has selected "Remember this browser" for two factor authentication purposes, as an asynchronous operation.</summary>
        /// <param name="user">The user entity.</param>
        Task RememberTwoFactorClientAsync(TUser user);
    }
}

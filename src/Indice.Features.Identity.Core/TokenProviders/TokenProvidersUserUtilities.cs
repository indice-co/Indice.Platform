using System.Globalization;
using System.Text;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.TokenProviders;

internal static class TokenProvidersUserUtilities
{
    /// <summary>Gets the security token for a user.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <param name="user">The user.</param>
    /// <param name="purpose">The purpose for which the token is being generated.</param>
    /// <param name="userManager">The user manager.</param>
    /// <returns>The security token.</returns>
    internal static async Task<byte[]> GetSecurityToken<TUser>(this TUser user, string purpose, UserManager<TUser> userManager) where TUser : User {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(user);
        // TODO: We will need to add entropy to the generated token
        //       in order to distinguish between two tokens generated for the same user and purpose.
        //       but different device id. That is why we have created this method which for the time being
        //       is just a wrapper around the existing method.
        return await userManager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
    }

}

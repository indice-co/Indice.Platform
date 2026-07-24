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
        var securityToken = await userManager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
        if (!string.Equals(purpose, "TwoFactor", StringComparison.Ordinal)) {
            return securityToken;
        }
        if (user.LastSignInDate == null) {
            return securityToken;
        }
        var timeStamp = Encoding.UTF8.GetBytes(user.LastSignInDate.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        byte[] token = new byte[securityToken.Length + timeStamp.Length];

        Buffer.BlockCopy(securityToken, 0, token, 0, securityToken.Length);
        Buffer.BlockCopy(timeStamp, 0, token, securityToken.Length, timeStamp.Length);
        return token;
    }

}

using System.Globalization;
using System.Security;
using System.Text;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.TokenProviders;

/// <summary>TokenProvider that generates tokens from the user's security stamp and notifies a user via email.</summary>
/// <typeparam name="TUser"></typeparam>
public class ExtendedEmailTokenProvider<TUser> : EmailTokenProvider<TUser> where TUser : User
{
    private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;

    /// <summary>Creates a new instance of <see cref="ExtendedEmailTokenProvider{TUser}"/>.</summary>
    /// <param name="totpOptions">Configuration used in <see cref="ExtendedEmailTokenProvider{TUser}"/> service.</param>
    public ExtendedEmailTokenProvider(IOptions<EmailTokenProviderTotpOptions> totpOptions) {
        _rfc6238AuthenticationService = new Rfc6238AuthenticationService(totpOptions.Value.Timestep, totpOptions.Value.CodeLength);
    }

    /// <inheritdoc />
    public async override Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user) {
        if (manager is null) {
            throw new ArgumentNullException(nameof(manager));
        }
        var token = await GetSecurityToken(purpose, manager, user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);
        return _rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static async Task<byte[]> GetSecurityToken(string purpose, UserManager<TUser> userManager, TUser user) {
        var securityToken = await userManager.CreateSecurityTokenAsync(user);
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
    /// <inheritdoc />
    public async override Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user) {
        if (manager is null) {
            throw new ArgumentNullException(nameof(manager));
        }
        if (!int.TryParse(token, out var code)) {
            return false;
        }
        var securityToken = await GetSecurityToken(purpose, manager, user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);
        return securityToken != null && _rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }
}

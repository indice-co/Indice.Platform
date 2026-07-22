using System.Globalization;
using System.Security;
using System.Text;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.TokenProviders;

/// <summary>
/// TokenProvider that generates tokens from the user's security stamp and notifies a user via email. This provider is an extended version of the <see cref="PhoneNumberTokenProvider{TUser}"/> which has a
/// configurable duration for the generated one-time password code.
/// </summary>
/// <typeparam name="TUser">The type used to represent a user.</typeparam>
public class ExtendedPhoneNumberTokenProvider<TUser> : PhoneNumberTokenProvider<TUser> where TUser : User
{
    private readonly Rfc6238AuthenticationService _rfc6238AuthenticationService;

    /// <summary>Creates a new instance of <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/>.</summary>
    /// <param name="totpOptions">Configuration used in <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/> service.</param>
    public ExtendedPhoneNumberTokenProvider(IOptions<PhoneNumberTokenProviderTotpOptions> totpOptions) {
        _rfc6238AuthenticationService = new Rfc6238AuthenticationService(totpOptions.Value.Timestep, totpOptions.Value.CodeLength);
    }

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> userManager, TUser user) {
        if (userManager is null) {
            throw new ArgumentNullException(nameof(userManager));
        }

        var securityToken = await GetSecurityToken(purpose, userManager, user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, userManager, user).ConfigureAwait(false);
        return _rfc6238AuthenticationService.GenerateCode(securityToken, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }


    private static async Task<byte[]> GetSecurityToken(string purpose, UserManager<TUser> userManager, TUser user) {
        var securityToken = await userManager.CreateSecurityTokenAsync(user);
        if (purpose != "TwoFactor") {
            return securityToken;
        }
        var timeStamp = Encoding.Unicode.GetBytes((user.LastSignInDate ?? DateTime.UtcNow).ToString("yyyyMMddHHmmsss"));
        byte[] token = new byte[securityToken.Length + timeStamp.Length];

        Buffer.BlockCopy(timeStamp, 0, token, 0, timeStamp.Length);
        Buffer.BlockCopy(timeStamp, 0, token, timeStamp.Length, timeStamp.Length);
        return token;
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> userManager, TUser user) {
        if (userManager is null) {
            throw new ArgumentNullException(nameof(userManager));
        }
        if (!int.TryParse(token, out var code)) {
            return false;
        }
        var securityToken = await GetSecurityToken(purpose, userManager, user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, userManager, user).ConfigureAwait(false);
        return securityToken is not null && _rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }
}

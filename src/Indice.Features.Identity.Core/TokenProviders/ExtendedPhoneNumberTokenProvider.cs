using System.Globalization;
using System.Security;
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
        var token = await userManager.CreateSecurityTokenAsync(user);
        var modifier = await GetUserModifierAsync(purpose, userManager, user);
        return _rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> userManager, TUser user) {
        if (userManager is null) {
            throw new ArgumentNullException(nameof(userManager));
        }
        if (!int.TryParse(token, out var code)) {
            return false;
        }
        var securityToken = await userManager.CreateSecurityTokenAsync(user);
        var modifier = await GetUserModifierAsync(purpose, userManager, user);
        return securityToken is not null && _rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }
}

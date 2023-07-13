using System.Globalization;
using System.Security;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.TokenProviders;

/// <summary>
/// TokenProvider that generates tokens from the user's security stamp and notifies a user via email. This provider is an extended version of the <see cref="PhoneNumberTokenProvider{TUser}"/> which has a
/// configurable duration for the generated one-time password code.
/// </summary>
/// <typeparam name="TUser">The type used to represent a user.</typeparam>
public class ExtendedPhoneNumberTokenProvider<TUser> : PhoneNumberTokenProvider<TUser> where TUser : User
{
    /// <summary>Creates a new instance of <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/>.</summary>
    /// <param name="rfc6238AuthenticationService">Time-Based One-Time Password Algorithm service.</param>
    public ExtendedPhoneNumberTokenProvider(Rfc6238AuthenticationService rfc6238AuthenticationService) {
        Rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
    }

    /// <summary>Time-Based One-Time Password Algorithm service.</summary>
    public Rfc6238AuthenticationService Rfc6238AuthenticationService { get; set; }

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> userManager, TUser user) {
        if (userManager == null) {
            throw new ArgumentNullException(nameof(userManager));
        }
        var token = await userManager.CreateSecurityTokenAsync(user);
        var modifier = await GetUserModifierAsync(purpose, userManager, user);
        return Rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> userManager, TUser user) {
        if (userManager == null) {
            throw new ArgumentNullException(nameof(userManager));
        }
        if (!int.TryParse(token, out var code)) {
            return false;
        }
        var securityToken = await userManager.CreateSecurityTokenAsync(user);
        var modifier = await GetUserModifierAsync(purpose, userManager, user);
        return securityToken is not null && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }
}

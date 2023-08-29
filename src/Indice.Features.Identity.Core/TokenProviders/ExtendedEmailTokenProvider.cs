using System.Globalization;
using System.Security;
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
        var token = await manager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);
        return _rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public async override Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user) {
        if (manager is null) {
            throw new ArgumentNullException(nameof(manager));
        }
        if (!int.TryParse(token, out var code)) {
            return false;
        }
        var securityToken = await manager.CreateSecurityTokenAsync(user).ConfigureAwait(false);
        var modifier = await GetUserModifierAsync(purpose, manager, user).ConfigureAwait(false);
        return securityToken != null && _rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
    }
}

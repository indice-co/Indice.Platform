using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.TokenProviders;

/// <summary>Token provider that generates tokens for a user that has the role of a developer. Used to support scenarios for local development or test environments.</summary>
/// <typeparam name="TUser">The type used to represent a user.</typeparam>
public class DeveloperPhoneNumberTokenProvider<TUser> : ExtendedPhoneNumberTokenProvider<TUser> where TUser : User
{
    private readonly TotpOptions _options;
    private readonly IHostEnvironment _environment;

    /// <summary>Creates a new instance of <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/>.</summary>
    /// <param name="options"></param>
    /// <param name="environment"></param>
    public DeveloperPhoneNumberTokenProvider(
        IOptions<TotpOptions> options,
        IHostEnvironment environment
    ) : base(Options.Create(new PhoneNumberTokenProviderTotpOptions {
        CodeDuration = options.Value.CodeDuration,
        CodeLength = options.Value.CodeLength
    })) {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    private bool EnableDeveloperTotp => _options.EnableDeveloperTotp && !_environment.IsProduction();

    /// <inheritdoc />
    public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> userManager, TUser user) {
        if (EnableDeveloperTotp) {
            var userClaims = await userManager.GetClaimsAsync(user);
            var developerTotpClaim = userClaims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.DeveloperTotp);
            var hasDeveloperTotp = developerTotpClaim is not null && await userManager.IsInRoleAsync(user, BasicRoleNames.Developer);
            if (hasDeveloperTotp) {
                return true;
            }
        }
        return await base.CanGenerateTwoFactorTokenAsync(userManager, user);
    }

    /// <inheritdoc />
    public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> userManager, TUser user) {
        if (EnableDeveloperTotp) {
            var userClaims = await userManager.GetClaimsAsync(user);
            var developerTotpClaim = userClaims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.DeveloperTotp);
            if (!string.IsNullOrWhiteSpace(developerTotpClaim?.Value)) {
                return developerTotpClaim.Value;
            }
        }
        return await base.GenerateAsync(purpose, userManager, user);
    }

    /// <inheritdoc />
    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> userManager, TUser user) {
        if (EnableDeveloperTotp) {
            var userClaims = await userManager.GetClaimsAsync(user);
            var developerTotpClaim = userClaims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.DeveloperTotp);
            if (developerTotpClaim?.Value == token) {
                return true;
            }
        }
        return await base.ValidateAsync(purpose, token, userManager, user);
    }
}

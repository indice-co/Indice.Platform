using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core.Totp;

/// <summary></summary>
/// <typeparam name="TUser">The type of user entity.</typeparam>
public sealed class TotpServiceDeveloper<TUser> : TotpServiceUser<TUser> where TUser : User
{
    /// <summary>Creates a new instance of <see cref="TotpServiceDeveloper{TUser}"/>.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="TotpServiceDeveloper{TUser}"/>.</param>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TotpServiceDeveloper(
        ExtendedUserManager<TUser> userManager,
        IStringLocalizer<TotpServiceDeveloper<TUser>> localizer,
        IServiceProvider serviceProvider
    ) : base(userManager, localizer, serviceProvider) { }

    /// <inheritdoc />
    public override async Task<TotpResult> SendAsync(
        TUser user,
        string message,
        TotpDeliveryChannel channel = TotpDeliveryChannel.Sms,
        string subject = null,
        string purpose = null,
        string pushNotificationClassification = null,
        string pushNotificationData = null,
        string tokenProvider = null,
        string authenticationMethod = null,
        string emailTemplate = null
    ) {
        if (user is null) {
            throw new ArgumentNullException(nameof(user), "User is null.");
        }
        var userClaims = await UserManager.GetClaimsAsync(user);
        var developerTotpClaim = userClaims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.DeveloperTotp);
        if (developerTotpClaim is not null) {
            return TotpResult.SuccessResult;
        }
        return await base.SendAsync(user, message, channel, subject, purpose, pushNotificationClassification, pushNotificationData, tokenProvider, authenticationMethod, emailTemplate);
    }

    /// <inheritdoc />
    public override async Task<TotpResult> VerifyAsync(
        TUser user,
        string code,
        string purpose = null,
        string tokenProvider = null
    ) {
        if (user is null) {
            throw new ArgumentNullException(nameof(user), "User is null.");
        }
        var userClaims = await UserManager.GetClaimsAsync(user);
        var developerTotpClaim = userClaims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.DeveloperTotp);
        if (developerTotpClaim?.Value == code) {
            return TotpResult.SuccessResult;
        }
        return await base.VerifyAsync(user, code, purpose, tokenProvider);
    }

    /// <inheritdoc />
    public override async Task<Dictionary<string, TotpProviderMetadata>> GetProvidersAsync(TUser user) {
        if (user is null) {
            throw new ArgumentNullException(nameof(user), "User is null.");
        }
        var isDeveloper = await UserManager.IsInRoleAsync(user, BasicRoleNames.Developer);
        if (!isDeveloper) {
            return await base.GetProvidersAsync(user);
        }
        var metadata = new TotpProviderMetadata {
            Type = TotpProviderType.StandardOtp,
            Channel = TotpDeliveryChannel.None,
            DisplayName = "Standard OTP",
            CanGenerate = false
        };
        return new Dictionary<string, TotpProviderMetadata> {
            { metadata.Name, metadata }
        };
    }
}

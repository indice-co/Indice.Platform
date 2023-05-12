using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.Server.Mfa.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Identity.Server.Mfa;

internal static class MfaApiHandlers
{
    internal static async Task<NoContent> SendPushNotification(
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        MfaLoginPushNotificationModel request
    ) {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException();
        var totpService = totpServiceFactory.Create<User>();
        await totpService.SendAsync(message =>
            message.ToUser(user)
                   .WithMessage("Your OTP code for login is: {0}")
                   .UsingPushNotification()
                   .WithSubject("OTP login")
                   .WithData(new { request.ConnectionId })
                   .WithPurpose(TotpConstants.TokenGenerationPurpose.MultiFactorAuthentication)
                   .WithClassification("MFA-Approval")
        );
        return TypedResults.NoContent();
    }
}

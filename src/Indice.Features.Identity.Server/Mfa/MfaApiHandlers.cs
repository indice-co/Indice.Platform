using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.Server.Mfa.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;

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
                   .WithPurpose("TwoFactor")
                   .WithClassification("MFA-Approval")
        );
        return TypedResults.NoContent();
    }

    internal static async Task<NoContent> ApproveMfaLogin(
        IAuthenticationMethodProvider authenticationMethodProvider,
        ApproveMfaLoginModel request
    ) {
        if (!authenticationMethodProvider.SignalREnabled) {
            throw new InvalidOperationException("SignalR service is not configured.");
        }
        await authenticationMethodProvider
            .HubContext!
            .Clients
            .Client(request.ConnectionId ?? throw new ArgumentNullException(nameof(request), "SignalR connection id is not present in the request."))
            .SendAsync(nameof(IMultiFactorAuthenticationHub.LoginApproved), request.Otp);
        return TypedResults.NoContent();
    }

    internal static async Task<NoContent> RejectMfaLogin(
        IAuthenticationMethodProvider authenticationMethodProvider,
        RejectMfaLoginModel request
    ) {
        if (!authenticationMethodProvider.SignalREnabled) {
            throw new InvalidOperationException("SignalR service is not configured.");
        }
        await authenticationMethodProvider
            .HubContext!
            .Clients
            .Client(request.ConnectionId ?? throw new ArgumentNullException(nameof(request), "SignalR connection id is not present in the request."))
            .SendAsync(nameof(IMultiFactorAuthenticationHub.LoginRejected));
        return TypedResults.NoContent();
    }
}

using System.Security.Claims;
using System.Threading.Channels;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.Server.Totp.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Server.Totp;

internal static class TotpHandlers
{
    internal static async Task<Results<NoContent, ForbidHttpResult, StatusCodeHttpResult, ValidationProblem>> Send(
        TotpServiceFactory totpServiceFactory,
        ClaimsPrincipal currentUser,
        IAuthenticationMethodProvider authenticationMethodProvider,
        TotpRequest request
    ) {
        var userId = currentUser.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return TypedResults.Forbid();
        }
        var result = default(TotpResult);
        var channel = TotpDeliveryChannel.None;
        string? tokenProvider = null;
        if (!string.IsNullOrWhiteSpace(request.AuthenticationMethod)) {
            var authenticationMethod = (await authenticationMethodProvider.GetAllMethodsAsync())
                .Where(x => x.DisplayName.Equals(request.AuthenticationMethod, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (authenticationMethod is null) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.AuthenticationMethod), $"Authentication method '{request.AuthenticationMethod}' is not configured in the system."));
            }
            if (!authenticationMethod.SupportsDeliveryChannel() || !authenticationMethod.SupportsTokenProvider()) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.AuthenticationMethod), $"Authentication method '{request.AuthenticationMethod}' must support a delivery channel and a token provider."));
            }
            channel = authenticationMethod.GetDeliveryChannel();
            tokenProvider = authenticationMethod.GetTokenProvider();
        } else {
            channel = request.Channel ?? TotpDeliveryChannel.None;
        }
        var totpService = totpServiceFactory.Create<User>();
        switch (channel) {
            case TotpDeliveryChannel.Sms:
            case TotpDeliveryChannel.Viber:
                result = await totpService.SendAsync(totp => totp
                    .ToPrincipal(currentUser)
                    .WithMessage(request.Message)
                    .UsingDeliveryChannel(channel)
                    .WithSubject(request.Subject)
                    .WithPurpose(request.Purpose)
                    .UsingTokenProvider(tokenProvider));
                break;
            case TotpDeliveryChannel.PushNotification:
                result = await totpService.SendAsync(totp => totp
                    .ToPrincipal(currentUser)
                    .WithMessage(request.Message)
                    .UsingPushNotification()
                    .WithSubject(request.Subject)
                    .WithPurpose(request.Purpose)
                    .UsingTokenProvider(tokenProvider)
                    .WithData(request.Data)
                    .WithClassification(request.Classification));
                break;
            case TotpDeliveryChannel.Email:
                result = await totpService.SendAsync(totp => totp.ToPrincipal(currentUser)
                    .WithMessage(request.Message)
                    .UsingEmail(request.EmailTemplate)
                    .WithSubject(request.Subject)
                    .WithPurpose(request.Purpose)
                    .WithData(request.Data)
                    .UsingTokenProvider(tokenProvider));
                break;
            case TotpDeliveryChannel.Telephone:
            default:
                return TypedResults.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }
        if (!result.Success) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.Channel), result.Error ?? "An error occurred."));
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, ForbidHttpResult, ValidationProblem>> Verify(
        TotpServiceFactory totpServiceFactory,
        ClaimsPrincipal currentUser,
        IStringLocalizer<TotpServiceUser<User>> localizer,
        IAuthenticationMethodProvider authenticationMethodProvider,
        TotpVerificationRequest request
    ) {
        var userId = currentUser.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return TypedResults.Forbid();
        }
        string? tokenProvider = null;
        if (!string.IsNullOrWhiteSpace(request.AuthenticationMethod)) {
            var authenticationMethod = (await authenticationMethodProvider.GetAllMethodsAsync())
                .Where(x => x.DisplayName.Equals(request.AuthenticationMethod, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            if (authenticationMethod is null) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.AuthenticationMethod), $"Authentication method '{request.AuthenticationMethod}' is not configured in the system."));
            }
            if (!authenticationMethod.SupportsTokenProvider()) {
                return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.AuthenticationMethod), $"Authentication method '{request.AuthenticationMethod}' must support a token provider."));
            }
            tokenProvider = authenticationMethod.GetTokenProvider();
        }
        var result = await totpServiceFactory.Create<User>().VerifyAsync(currentUser, request.Code, request.Purpose, tokenProvider);
        if (!result.Success) {
            var errors = ValidationErrors.AddError(nameof(request.Code), localizer["Invalid code"]);
            return TypedResults.ValidationProblem(errors);
        }
        return TypedResults.NoContent();
    }
}

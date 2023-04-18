using System.Security.Claims;
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
        TotpRequest request
    ) {
        var userId = currentUser.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return TypedResults.Forbid();
        }
        var result = default(TotpResult);
        var totpService = totpServiceFactory.Create<User>();
        switch (request.Channel) {
            case TotpDeliveryChannel.Sms:
            case TotpDeliveryChannel.Viber:
                result = await totpService.SendAsync(totp =>
                    totp.ToPrincipal(currentUser)
                        .WithMessage(request.Message)
                        .UsingDeliveryChannel(request.Channel)
                        .WithPurpose(request.Purpose)
                );
                break;
            case TotpDeliveryChannel.PushNotification:
                result = await totpService.SendAsync(totp => totp
                    .ToPrincipal(currentUser)
                    .WithMessage(request.Message)
                    .UsingPushNotification()
                    .WithSubject(request.Subject)
                    .WithPurpose(request.Purpose)
                    .WithData(request.Data)
                    .WithClassification(request.Classification)
                );
                break;
            case TotpDeliveryChannel.Email:
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
        TotpVerificationRequest request
    ) {
        var userId = currentUser.FindSubjectId();
        if (string.IsNullOrEmpty(userId)) {
            return TypedResults.Forbid();
        }
        var result = await totpServiceFactory.Create<User>().VerifyAsync(currentUser, request.Code, request.Purpose);
        if (!result.Success) {
            var errors = ValidationErrors.AddError(nameof(request.Code), localizer["Invalid code"]);
            return TypedResults.ValidationProblem(errors);
        }
        return TypedResults.NoContent();
    }
}

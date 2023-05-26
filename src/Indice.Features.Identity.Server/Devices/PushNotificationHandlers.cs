using Humanizer;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Identity.Server.Devices;
internal static class PushNotificationHandlers
{
    internal static async Task<Results<NoContent, ValidationProblem>> SendPushNotification(
        IPushNotificationService pushNotificationService,
        SendPushNotificationRequest request
    ) {
        var errors = ValidationErrors.Create();
        if (string.IsNullOrWhiteSpace(request.Title)) {
            errors.AddError(nameof(request.Title).Camelize(), "Please provide a message title.");
        }
        var broadcast = request.Broadcast.GetValueOrDefault();
        if (!broadcast && string.IsNullOrWhiteSpace(request.UserTag)) {
            errors.AddError(nameof(request.Title).Camelize(), "Please provide a user tag.");
        }
        if (errors.Count > 0) {
            TypedResults.ValidationProblem(errors, detail: "Model validation failed");
        }
        if (broadcast) {
            await pushNotificationService.BroadcastAsync(request.Title, request.Body, request.Data, request.Classification);
        } else {
            await pushNotificationService.SendToUserAsync(request.Title, request.Body, request.Data, request.UserTag, request.Classification, request.Tags ?? Array.Empty<string>());
        }
        return TypedResults.NoContent();
    }
}

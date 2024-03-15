using System.Security.Claims;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminNotificationsHandler
{
    public static async Task<Results<Ok<NotificationSubscriptionResponse>, BadRequest>> GetMySubscriptions(ClaimsPrincipal User, AdminCasesApiOptions casesApiOptions, INotificationSubscriptionService service) {
        var options = new ListOptions<NotificationFilter> {
            Filter = NotificationFilter.FromUser(User, casesApiOptions.GroupIdClaimType)
        };
        var result = await service.GetSubscriptions(options);
        if (result == null) {
            return TypedResults.BadRequest();
        }
        return TypedResults.Ok(new NotificationSubscriptionResponse {
            NotificationSubscriptions = result
        });
    }

    public static async Task<Results<NoContent, BadRequest>> Subscribe(NotificationSubscriptionRequest request, ClaimsPrincipal User, AdminCasesApiOptions casesApiOptions, INotificationSubscriptionService service) {
        await service.Subscribe(request.CaseTypeIds, NotificationSubscription.FromUser(User, casesApiOptions.GroupIdClaimType));
        return TypedResults.NoContent();
    }
}

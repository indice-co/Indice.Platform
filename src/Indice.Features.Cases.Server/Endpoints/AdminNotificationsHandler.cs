using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminNotificationsHandler
{
    public static async Task<Ok<NotificationSubscriptionResponse>> GetMySubscriptions(ClaimsPrincipal user, IOptions<CaseServerOptions> casesOptions, INotificationSubscriptionService service) {
        var options = new ListOptions<NotificationFilter> {
            Filter = NotificationFilter.FromUser(user, casesOptions.Value.GroupIdClaimType)
        };
        var result = await service.GetSubscriptions(options);
        return TypedResults.Ok(new NotificationSubscriptionResponse {
            NotificationSubscriptions = result
        });
    }

    public static async Task<NoContent> Subscribe(NotificationSubscriptionRequest request, ClaimsPrincipal user, IOptions<CaseServerOptions> casesOptions, INotificationSubscriptionService service) {
        await service.Subscribe(request.CaseTypeIds ?? [], NotificationSubscription.FromUser(user, casesOptions.Value.GroupIdClaimType));
        return TypedResults.NoContent();
    }
}

using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminNotificationsHandlers
{
    public static async Task<Ok<ResultSet<NotificationSubscription>>> GetMySubscriptions(
        ClaimsPrincipal User, 
        IOptions<CaseServerOptions> casesOptions, 
        INotificationSubscriptionService service
    ) {
        var options = new ListOptions<NotificationFilter> {
            Filter = NotificationFilter.FromUser(User, casesOptions.Value.GroupIdClaimType)
        };

        var result = await service.GetSubscribers(options);
        return TypedResults.Ok(result);
    }

    public static async Task<Results<NoContent, ValidationProblem>> Subscribe(
        NotificationSubscriptionRequest request,
        ClaimsPrincipal User,
        IOptions<CaseServerOptions> casesOptions,
        INotificationSubscriptionService service
    ) {
        await service.Subscribe(Subscriber.FromUser(User, casesOptions.Value.GroupIdClaimType), request.CaseTypeIds!);
        return TypedResults.NoContent();
    }
}

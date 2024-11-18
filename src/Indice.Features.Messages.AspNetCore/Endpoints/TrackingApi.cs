#if NET7_0_OR_GREATER
#nullable enable

using Indice.Features.Messages.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;
using Indice.Types;

namespace Microsoft.AspNetCore.Routing;
/// <summary>
/// Provides endpoints for managing tracking.
/// </summary>
public static class TrackingApi
{
    /// <summary>Registers the endpoints for Tracking API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapTracking(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageInboxOptions>>().Value;
        var group = routes.MapGroup("_tracking");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Tracking");

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser());

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2");

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("messages/cta/{trackingCode}", TrackingHandlers.Track)
             .WithName(nameof(TrackingHandlers.Track))
             .WithSummary("Tracks a campaign message click and redirects to the action link.")
             .WithDescription(TrackingHandlers.TRACK_DESCRIPTION)
             .AllowAnonymous();

        return group;
    }
}

#nullable disable
#endif
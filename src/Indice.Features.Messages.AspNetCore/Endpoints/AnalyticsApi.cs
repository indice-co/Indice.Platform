using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing campaign-related operations, including retrieving, creating, updating, publishing, 
/// and deleting campaigns, as well as handling attachments and statistics.
/// </summary>
internal static class AnalyticsApi
{
    /// <summary>Registers the endpoints for Campaigns API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapAnalytics(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/analytics");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Analytics");
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser()
                                           .RequireCampaignsManagement()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("overview", AnalyticsHandlers.GetOverview)
             .WithName(nameof(AnalyticsHandlers.GetOverview))
             .WithSummary("Gets the overview analytics.");

        group.MapGet("events", AnalyticsHandlers.GetEventsList)
             .WithName(nameof(AnalyticsHandlers.GetEventsList))
             .WithSummary("Gets a result set of events.");

        group.MapGet("events/series", AnalyticsHandlers.GetEventsSeriesList)
             .WithName(nameof(AnalyticsHandlers.GetEventsSeriesList))
             .WithSummary("Gets a result set of events.");

        return group;
    }
}
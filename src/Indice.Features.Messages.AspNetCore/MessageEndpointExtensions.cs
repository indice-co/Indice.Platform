#nullable enable
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;
/// <summary>Configuration extensions on <seealso cref="IEndpointRouteBuilder "/>.</summary>
public static class MessageEndpointExtensions
{

    /// <summary>Registers the endpoints for the Messaging Api.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for further configuration.</returns>
    public static IEndpointRouteBuilder MapMessaging(this IEndpointRouteBuilder routes) {
        // my messages / inbox api
        routes.MapMessagingInbox();
        // management
        routes.MapMessagingManagement();
        return routes;
    }

    /// <summary>Registers the endpoints for the Messaging Api <strong>Only the inbox part.</strong></summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for further configuration.</returns>
    /// <remarks>For everything use <see cref="MapMessaging(IEndpointRouteBuilder)"/> instead</remarks>
    public static IEndpointRouteBuilder MapMessagingInbox(this IEndpointRouteBuilder routes) {
        // my messages / inbox api
        routes.MapMyMessages();
        routes.MapTracking();
        return routes;
    }

    /// <summary>Registers the endpoints for the Messaging Api <strong>Only the management part.</strong></summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for further configuration.</returns>
    /// <remarks>For everything use <see cref="MapMessaging(IEndpointRouteBuilder)"/> instead</remarks>
    public static IEndpointRouteBuilder MapMessagingManagement(this IEndpointRouteBuilder routes) {
        // management
        routes.MapCampaigns();
        routes.MapContacts();
        routes.MapDistributionLists();
        routes.MapMessageSenders();
        routes.MapMessageTypes();
        routes.MapTemplates();
        return routes;
    }
}
#nullable disable
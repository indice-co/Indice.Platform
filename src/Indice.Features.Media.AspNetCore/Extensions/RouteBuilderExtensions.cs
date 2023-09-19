using Indice.Features.Media.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Configuration extensions on <seealso cref="IEndpointRouteBuilder "/>.</summary>
public static class RouteBuilderExtensions
{

    /// <summary>Registers the endpoints for the Media Api.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    public static IEndpointRouteBuilder MapMediaLibrary(this IEndpointRouteBuilder builder) {
        builder.MapMedia();
        builder.MapFolders();
        builder.MapMediaSettings();
        return builder;
    }
}

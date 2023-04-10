using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Configuration extensions on <seealso cref="IEndpointRouteBuilder "/>.</summary>
public static class IdentityServerEndpointRouteBuilderExtensions
{
    /// <summary>Maps all Indice Identity Server API endpoints.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IdentityServerEndpointRouteBuilder MapIdentityServer(this IEndpointRouteBuilder routes) {
        var builder = new IdentityServerEndpointRouteBuilder(routes);
        // Indice Identity Server endpoints.
        builder.MapManageUsers();
        builder.MapManageClients();
        builder.MapManageRoles();
        builder.MapManageClaimTypes();
        builder.MapManageDashboard();
        builder.MapManageLookups();
        builder.MapManageResources();
        builder.MapMyAccount();
        // Devices and push notifications endpoints.
        builder.MapMyDevices();
        builder.MapDevicePush();
        // TOTP API
        builder.MapTotps();
        // Sign in logs endpoints.
        builder.MapSignInLogs();
        // Database settings endpoints.
        builder.MapDatabaseSettings();
        return builder;
    }

    /// <summary>Gets the instance of <see cref="ExtendedEndpointOptions"/>.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>\
    public static ExtendedEndpointOptions GetEndpointOptions(this IEndpointRouteBuilder routes) => routes.GetEndpointOptions<ExtendedEndpointOptions>();

    /// <summary>Get an instance of the provided options type.</summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static TOptions GetEndpointOptions<TOptions>(this IEndpointRouteBuilder routes) where TOptions : class, new() => routes.ServiceProvider.GetService<IOptions<TOptions>>()?.Value ?? new TOptions();
}

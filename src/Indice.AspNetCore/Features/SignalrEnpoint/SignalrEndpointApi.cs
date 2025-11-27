using Indice.Security;
using Indice.SignalR.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.SignalR.Management;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Endpoint mappings for SignalR.</summary>
public static class SignalrEndpointApi
{

    public static IHostApplicationBuilder AddSignalREndpoints(this IHostApplicationBuilder builder)
    {
        // Use SignalR SDK to create a new HubContext and a negotiation response
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(serviceManagerOptions =>
            {
                serviceManagerOptions.ConnectionString = builder.Configuration["AzureSignalRConnectionString"];
                serviceManagerOptions.ServiceTransportType = ServiceTransportType.Transient;
            })
            .BuildServiceManager();
        return builder;
    }

    /// <summary>Maps SignalR endpoints.</summary>
    /// <param name="routes">The endpoint route builder.</param>
    public static void MapSignalrEndpoints(this IEndpointRouteBuilder routes)
    {

        var options = routes.ServiceProvider.GetRequiredService<IOptions<SignalREndpointsOptions>>().Value;
        var group = routes.MapGroup($"{options.EndpointRoutePattern}");
        if (!string.IsNullOrEmpty(options.GroupName))
        {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Templates");
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(options.AuthenticationScheme)
                                           .RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.AddOpenApiSecurityRequirement("oauth2", allowedScopes)
                .WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        if (options.ExcludeFromDescription)
        {
            group.ExcludeFromDescription();
        }
        routes.MapPost("/{hub}/negotiate", SignalREndpointsHandler.Negotiate)
            .WithDescription("Get SignalR negotiate information")
            .WithName(nameof(SignalREndpointsHandler.Negotiate))
             .WithTags("SignalR");
        routes.MapPost("/{hub}/broadcast/users", SignalREndpointsHandler.BroadcastToUsers)
            .WithDescription("Broadcast message to all users")
            .WithName(nameof(SignalREndpointsHandler.BroadcastToUsers))
            .WithTags("SignalR");
        routes.MapPost("/{hub}/broadcast/users/{userId}", SignalREndpointsHandler.BroadcastToUser)
            .WithDescription("Broadcast message to a specific user")
            .WithName(nameof(SignalREndpointsHandler.BroadcastToUser))
            .WithTags("SignalR");
        
    }
}

public class SignalREndpointsOptions
{
    public string AuthenticationScheme { get; set; } = "Bearer";
    /// <summary>
    /// The endpoint route pattern defaults to <strong>"/translations.{lang:culture}.json"</strong>. If changes are made to the path we must paintain the lang parameter.
    /// </summary>
    public string EndpointRoutePattern { get; set; } = "/api/signalr";
    /// <summary>Optional group name for the endpoints.</summary>
    public string GroupName { get; set; } = "SignalR";
    /// <summary>Required scope to access the endpoints.</summary>
    public string RequiredScope { get; set; } = null!;
    /// <summary>
    /// Decides whether to enable swagger/openapi documentation for the endpoint
    /// </summary>
    public bool ExcludeFromDescription { get; set; } = true;
}
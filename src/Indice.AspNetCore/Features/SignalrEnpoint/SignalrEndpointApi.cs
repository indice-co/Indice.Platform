using System.Collections.Concurrent;
using Azure.Core.Serialization;
using Indice.AspNetCore.Features.SignalrEnpoint;
using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Indice.Security;
using Indice.Serialization;
using Indice.SignalR.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static Duende.IdentityModel.ClaimComparer;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Endpoint mappings for SignalR.</summary>
public static class SignalrEndpointApi
{

    /// <summary>
    /// Adds SignalR endpoint services to the application.
    /// </summary>
    /// <param name="builder"></param>
    public static IHostApplicationBuilder AddSignalREndpoints(this IHostApplicationBuilder builder)
    {
        // Use SignalR SDK to create a new HubContext and a negotiation response
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(serviceManagerOptions =>
            {
                serviceManagerOptions.ConnectionString = builder.Configuration["AzureSignalRConnectionString"];
                serviceManagerOptions.ServiceTransportType = ServiceTransportType.Transient;
                serviceManagerOptions.UseJsonObjectSerializer(
                    new JsonObjectSerializer(JsonSerializerOptionDefaults.GetDefaultSettings()));
            })
            .BuildServiceManager();
        builder.Services.AddSingleton(serviceManager);
        var section = builder.Configuration.GetSection("SignalREndpoints");
        builder.Services.Configure<SignalREndpointsOptions>(section);
        builder.Services.TryAddTransient<ISignalRListenerService,SignalRListenerService>();
        builder.Services.TryAddSingleton<HubContextStore>();

        var options = section.Get<SignalREndpointsOptions>();
        if(options?.HasBroadcasting == true) {
            builder.Services.TryAddTransient<ISignalRBroadcastingService, SignalRBroadcastingService>();
        }
        return builder;
    }

    /// <summary>Maps SignalR endpoints.</summary>
    /// <param name="routes">The endpoint route builder.</param>
    public static RouteGroupBuilder MapSignalrEndpoints(this IEndpointRouteBuilder routes)
    {

        var options = routes.ServiceProvider.GetRequiredService<IOptions<SignalREndpointsOptions>>().Value;
        var group = routes.MapGroup($"{options.EndpointRoutePattern}");
        group.WithTags("SignalR");

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
        group.MapPost("/{hub}/negotiate", SignalREndpointsHandler.Negotiate)
            .WithDescription("Get SignalR negotiate information")
            .WithName(nameof(SignalREndpointsHandler.Negotiate));
        if (options.HasBroadcasting) {
            group.MapPost("/{hub}/broadcast/users", SignalREndpointsHandler.BroadcastToUsers)
                .WithDescription("Broadcast message to all users")
                .WithName(nameof(SignalREndpointsHandler.BroadcastToUsers));
            group.MapPost("/{hub}/broadcast/users/{userId}", SignalREndpointsHandler.BroadcastToUser)
                .WithDescription("Broadcast message to a specific user")
                .WithName(nameof(SignalREndpointsHandler.BroadcastToUser));
            group.MapPost("/{hub}/broadcast/groups/{groupId}", SignalREndpointsHandler.BroadcastToGroup)
                .WithDescription("Broadcast message to a specific group")
                .WithName(nameof(SignalREndpointsHandler.BroadcastToGroup));
        }

        return group;
    }
}


/// <summary>
/// Provides additional configurability for SignalR endpoints.
/// </summary>
public class SignalREndpointsOptions
{
    /// <summary>
    /// The authentication scheme used to secure the endpoints.
    /// </summary>
    public string AuthenticationScheme { get; set; } = "Bearer";
    /// <summary>
    /// The endpoint route pattern for the SignalR endpoints.
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
    /// <summary>
    /// List of Group Claims 
    /// </summary>
    public List<string> GroupClaims { get; set; } = new();

    /// <summary>
    /// List of allowed Hubs
    /// </summary>
    public List<string> AllowedHubs { get; set; } = new();

    /// <summary>
    /// A flag to indicate if broadcasting features are enabled.
    /// </summary>
    public bool HasBroadcasting { get; set; } = false;
}
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalREndpoint;

/// <summary>Endpoint mappings for SignalR.</summary>
public static class SignalREndpointsApi
{

    /// <summary>Maps SignalR endpoints.</summary>
    /// <param name="routes">The endpoint route builder.</param>
    public static RouteGroupBuilder MapSignalRProxy(this IEndpointRouteBuilder routes)
    {

        var options = routes.ServiceProvider.GetRequiredService<IOptions<SignalREndpointsOptions>>().Value;
        var group = routes.MapGroup($"{options.EndpointRoutePattern}");
        group.WithTags(options.TagName);

        var authenticationScheme = options.AuthenticationSchemes.Count > 0 ? options.AuthenticationSchemes[0] : "Bearer";
        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(authenticationScheme)
                                           .RequireAuthenticatedUser());

        if (options.ExcludeFromDescription)
        {
            //group.ExcludeFromDescription();
        }

        group.MapPost("/{hub}/negotiate", SignalREndpointsHandler.Negotiate)
            .WithDescription(SignalREndpointsHandler.NEGOTIATE)
            .WithName(nameof(SignalREndpointsHandler.Negotiate));
        group.MapPost("/{hub}/broadcastToUser", SignalREndpointsHandler.BroadcastToUser)
            .WithDescription(SignalREndpointsHandler.BROADCASTTOUSER)
            .WithName(nameof(SignalREndpointsHandler.BroadcastToUser));
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
    public List<string> AuthenticationSchemes { get; set; } = new();
    /// <summary>
    /// The endpoint route pattern for the SignalR endpoints.
    /// </summary>
    public string EndpointRoutePattern { get; set; } = "/api/signalr";
    /// <summary>Optional group name for the endpoints.</summary>
    public string GroupName { get; set; } = "SignalR";
    /// <summary>Optional tag name for the endpoints.</summary>
    public string TagName { get; set; } = "SignalR";
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
}
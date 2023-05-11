using Indice.Features.Identity.Core.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Extensions on <see cref="IEndpointRouteBuilder"/> interface.</summary>
public static class IEndpointRouteBuilderExtensions
{
    /// <summary>Maps the SignalR hub required for MFA.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application.</param>
    /// <param name="pattern">The desired URL pattern. Defaults to <i>/mfa</i>.</param>
    public static void MapMultiFactorAuthenticationHub(this IEndpointRouteBuilder builder, string pattern = "/mfa") {
        var configuration = builder.ServiceProvider.GetRequiredService<IConfiguration>();
        var signalRConnectionString = configuration.GetConnectionString("SignalRService");
        if (!string.IsNullOrWhiteSpace(signalRConnectionString)) {
            builder.MapHub<MultiFactorAuthenticationHub>($"/{pattern.Trim('/')}");
        }
    }
}

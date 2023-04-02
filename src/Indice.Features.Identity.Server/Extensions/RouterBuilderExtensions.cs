using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Configuration extensions on <seealso cref="IEndpointRouteBuilder "/>.
/// </summary>
public static class IdentityServerEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Map the identity server defaults
    /// </summary>
    /// <param name="routes">the aspnet core route builder</param>
    /// <returns>An instance of <see cref="IdentityServerEndpointRouteBuilder"/> to </returns>
    public static IdentityServerEndpointRouteBuilder MapIdentityServerEndpoints(this IEndpointRouteBuilder routes) {
        var builder = new IdentityServerEndpointRouteBuilder(routes);
        builder.MapUsers();
        builder.MapRoles();
        builder.MapClaimTypes();
        return builder; 
    }
    /// <summary>
    /// Get the <see cref="ExtendedEndpointOptions"/>
    /// </summary>
    /// <param name="routes">the aspnet core route builder</param>
    /// <returns>An instance of <see cref="ExtendedEndpointOptions"/> to </returns>
    public static ExtendedEndpointOptions GetEndpointOptions(this IEndpointRouteBuilder routes) => routes.ServiceProvider.GetService<IOptions<ExtendedEndpointOptions>>()?.Value;
}

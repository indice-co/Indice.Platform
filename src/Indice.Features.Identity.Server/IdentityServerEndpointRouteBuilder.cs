using Indice.Features.Identity.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Server;

/// <summary>
/// Scalefin route builder
/// </summary>
public class IdentityServerEndpointRouteBuilder : IEndpointRouteBuilder
{
    private readonly IEndpointRouteBuilder _innerBuilder;

    /// <summary>
    /// constructs a <see cref="IdentityServerEndpointRouteBuilder"/> given the default builder from AspNetCore
    /// </summary>
    /// <param name="endpointRouteBuilder">The aspnetcore default builder</param>
    public IdentityServerEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder) {
        _innerBuilder = endpointRouteBuilder ?? throw new ArgumentNullException(nameof(endpointRouteBuilder));
        Options = endpointRouteBuilder.ServiceProvider.GetRequiredService<IOptions<IdentityServerEndpointOptions>>()?.Value;
    }

    /// <summary>
    /// Scalefin Server Options
    /// </summary>
    public IdentityServerEndpointOptions Options { get; }
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider => _innerBuilder.ServiceProvider;
    /// <inheritdoc/>
    public ICollection<EndpointDataSource> DataSources => _innerBuilder.DataSources;
    /// <inheritdoc/>
    public IApplicationBuilder CreateApplicationBuilder() => _innerBuilder.CreateApplicationBuilder();
}

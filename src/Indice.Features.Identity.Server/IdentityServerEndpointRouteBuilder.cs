using Indice.Features.Identity.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Server;

/// <summary>Indice Identity Server route builder.</summary>
public class IdentityServerEndpointRouteBuilder : IEndpointRouteBuilder
{
    private readonly IEndpointRouteBuilder _innerBuilder;

    /// <summary>Constructs an <see cref="IdentityServerEndpointRouteBuilder"/> given the default framework builder.</summary>
    /// <param name="endpointRouteBuilder">The <see cref="IEndpointRouteBuilder"/> default route builder.</param>
    public IdentityServerEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder) {
        _innerBuilder = endpointRouteBuilder ?? throw new ArgumentNullException(nameof(endpointRouteBuilder));
        Options = endpointRouteBuilder.ServiceProvider.GetRequiredService<IOptions<ExtendedEndpointOptions>>()?.Value ?? new ExtendedEndpointOptions();
    }

    /// <summary>Indice Identity Server endpoints configuration options.</summary>
    public ExtendedEndpointOptions Options { get; }
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider => _innerBuilder.ServiceProvider;
    /// <inheritdoc/>
    public ICollection<EndpointDataSource> DataSources => _innerBuilder.DataSources;
    /// <inheritdoc/>
    public IApplicationBuilder CreateApplicationBuilder() => _innerBuilder.CreateApplicationBuilder();
}

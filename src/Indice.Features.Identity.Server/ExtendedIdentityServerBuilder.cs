using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server;

/// <summary>Builder for configuring the Indice Identity Server.</summary>
public interface IExtendedIdentityServerBuilder : IIdentityServerBuilder
{
    /// <summary>Gets the services.</summary>
    public IdentityBuilder IdentityBuilder { get; }
    /// <summary>Gets the Configuration.</summary>
    public IConfiguration Configuration { get; }
    /// <summary>The current environment.</summary>
    public IWebHostEnvironment Environment { get; }
}

/// <inheritdoc/>
internal class ExtendedIdentityServerBuilder : IExtendedIdentityServerBuilder
{
    /// <inheritdoc/>
    public ExtendedIdentityServerBuilder(IIdentityServerBuilder innerServerBuilder, IdentityBuilder innerIdentityBuilder, IConfiguration configuration, IWebHostEnvironment environment) {
        IdentityServerBuilder = innerServerBuilder ?? throw new ArgumentNullException(nameof(innerServerBuilder));
        IdentityBuilder = innerIdentityBuilder ?? throw new ArgumentNullException(nameof(innerIdentityBuilder));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        IdentityServerBuilder.Services.AddEndpointsApiExplorer();
        IdentityServerBuilder.Services.AddEndpointParameterFluentValidation(ServiceLifetime.Singleton);
    }

    public IIdentityServerBuilder IdentityServerBuilder { get; }
    public IdentityBuilder IdentityBuilder { get; }
    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }
    public IServiceCollection Services => IdentityServerBuilder.Services;
}

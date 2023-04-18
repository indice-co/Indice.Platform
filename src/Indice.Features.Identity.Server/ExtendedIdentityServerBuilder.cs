using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server;

/// <summary>Builder for configuring the Indice Identity Server.</summary>
public interface IExtendedIdentityServerBuilder : IIdentityServerBuilder
{
    /// <summary>Gets the configured <see cref="Microsoft.AspNetCore.Identity.IdentityBuilder"/>.</summary>
    public IdentityBuilder IdentityBuilder { get; }
    /// <summary>Gets the <see cref="IConfiguration"/> instance.</summary>
    public IConfiguration Configuration { get; }
    /// <summary>Gets the current <see cref="IWebHostEnvironment"/>.</summary>
    public IWebHostEnvironment Environment { get; }
}

/// <inheritdoc/>
internal class ExtendedIdentityServerBuilder : IExtendedIdentityServerBuilder
{
    /// <inheritdoc/>
    public ExtendedIdentityServerBuilder(
        IIdentityServerBuilder identityServerBuilder, 
        IdentityBuilder identityBuilder, 
        IConfiguration configuration, 
        IWebHostEnvironment environment
    ) {
        IdentityServerBuilder = identityServerBuilder ?? throw new ArgumentNullException(nameof(identityServerBuilder));
        IdentityBuilder = identityBuilder ?? throw new ArgumentNullException(nameof(identityBuilder));
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

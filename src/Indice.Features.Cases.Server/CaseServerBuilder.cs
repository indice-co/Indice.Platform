using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Server;

/// <summary>The builder interface to extend with configuration methods related to Case Management.</summary>
public interface ICaseServerBuilder
{
    /// <summary>Gets the <see cref="IServiceCollection"/> to configure.</summary>
    public IServiceCollection Services { get; }
    /// <summary>Gets the <see cref="IConfiguration"/> instance.</summary>
    public IConfiguration Configuration { get; }
    /// <summary>Gets the current <see cref="IWebHostEnvironment"/>.</summary>
    public IWebHostEnvironment Environment { get; }
}


internal class CaseServerBuilder : ICaseServerBuilder
{
    public CaseServerBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment) {
        Services = services;
        Configuration = configuration;
        Environment = environment;
    }

    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services.Configuration;

/// <summary>Provides a builder for configuring webhooks services.</summary>
public sealed class WebhooksBuilder(IServiceCollection services, IConfiguration configuration)
{
    /// <summary>Gets the <see cref="IServiceCollection"/>.</summary>
    public IServiceCollection Services { get; } = services;
    /// <summary>Gets the <see cref="IConfiguration"/>.</summary>   
    public IConfiguration Configuration { get; } = configuration;
}

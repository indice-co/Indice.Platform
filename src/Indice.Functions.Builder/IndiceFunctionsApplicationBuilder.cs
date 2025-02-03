using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A builder for web applications and services that has all the indice defaults preconfigured. This is a decorator for the inner <seealso cref="FunctionsApplicationBuilder"/>
/// </summary>
public class IndiceFunctionsApplicationBuilder : IHostBuilder
{
    private HostBuilder InnerBuilder { get; }
    

    /// <summary>
    /// constructs the <see cref="IndiceFunctionsApplicationBuilder "/> given the inner builder.
    /// </summary>
    /// <param name="innerBuilder"></param>
    internal IndiceFunctionsApplicationBuilder(HostBuilder innerBuilder) {
        InnerBuilder = innerBuilder;
    }

    /// <summary>
    /// Builds the <see cref="IHost"/> to the idice specifications.
    /// </summary>
    /// <returns>A configured <see cref="IHost"/>.</returns>
    public IHost Build() {
        var app = InnerBuilder.Build();
        return app;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IHostBuilder"/> class with preconfigured defaults.
    /// </summary>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    public static IHostBuilder CreateBuilder() {
        var builder = new HostBuilder();
        builder.ConfigureFunctionsDefaults();
        return new IndiceFunctionsApplicationBuilder(builder);
    }

    /// <inheritdoc/>
    public IDictionary<object, object> Properties => InnerBuilder.Properties;
    /// <inheritdoc/>
    public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => InnerBuilder.ConfigureAppConfiguration(configureDelegate);
    /// <inheritdoc/>
    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => InnerBuilder.ConfigureContainer(configureDelegate);
    /// <inheritdoc/>
    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => InnerBuilder.ConfigureHostConfiguration(configureDelegate);
    /// <inheritdoc/>
    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => InnerBuilder.ConfigureServices(configureDelegate);
    /// <inheritdoc/>
    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => InnerBuilder.UseServiceProviderFactory(factory);
    /// <inheritdoc/>
    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull => InnerBuilder.UseServiceProviderFactory(factory);
}

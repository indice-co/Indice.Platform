using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A builder for web applications and services that has all the indice defaults preconfigured. This is a decorator for the inner <seealso cref="FunctionsApplicationBuilder"/>
/// </summary>
public class IndiceFunctionsApplicationBuilder : IHostApplicationBuilder, IFunctionsWorkerApplicationBuilder
{
    private FunctionsApplicationBuilder InnerBuilder { get; }



    /// <summary>
    /// constructs the <see cref="IndiceFunctionsApplicationBuilder "/> given the inner builder.
    /// </summary>
    /// <param name="innerBuilder"></param>
    internal IndiceFunctionsApplicationBuilder(FunctionsApplicationBuilder innerBuilder) {
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
    public static IndiceFunctionsApplicationBuilder CreateBuilder(string[] args) {
        var builder = FunctionsApplication.CreateBuilder(args);
        builder.ConfigureFunctionsDefaults();
        return new IndiceFunctionsApplicationBuilder(builder);
    }


    /// <inheritdoc/>
    public IConfigurationManager Configuration => InnerBuilder.Configuration;
    /// <inheritdoc/>
    public IHostEnvironment Environment => InnerBuilder.Environment;
    /// <inheritdoc/>
    public ILoggingBuilder Logging => InnerBuilder.Logging;
    /// <inheritdoc/>
    public IMetricsBuilder Metrics => InnerBuilder.Metrics;
    /// <inheritdoc/>
    public IDictionary<object, object> Properties => InnerBuilder.Properties;
    /// <inheritdoc/>
    public IServiceCollection Services => InnerBuilder.Services;
    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
        => InnerBuilder.ConfigureContainer(factory, configure);

    /// <inheritdoc/>
    public IFunctionsWorkerApplicationBuilder Use(Func<FunctionExecutionDelegate, FunctionExecutionDelegate> middleware) => InnerBuilder.Use(middleware);

}

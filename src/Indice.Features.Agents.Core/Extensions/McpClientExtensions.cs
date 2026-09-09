using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>Provides extension methods for registering an <see cref="McpClient"/> in the dependency injection container.</summary>
public static class McpClientServiceCollectionExtensions
{
    /// <summary>The default name used for registering an <see cref="McpClient"/> in the dependency injection container.</summary>
    public const string DefaultName = "SharedMcpClient";

    /// <summary>Registers an <see cref="McpClient"/> in the dependency injection container with the default name.</summary>
    public static IMcpClientBuilder AddMcpClient(
        this IServiceCollection services,
        Action<McpClientOptions>? configure = null)
        => services.AddMcpClient(DefaultName, configure);

    /// <summary>Registers an <see cref="McpClient"/> in the dependency injection container with a specified name.</summary>
    public static IMcpClientBuilder AddMcpClient(
        this IServiceCollection services,
        string name,
        Action<McpClientOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        services.AddOptions();
        services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();

        services
            .AddOptions<McpClientRegistrationOptions>(name)
            .Configure(o => configure?.Invoke(o.Client));

        services.TryAddKeyedSingleton<IMcpClientFactory>(name, (sp, key) => {
            var options = sp.GetRequiredService<IOptionsMonitor<McpClientRegistrationOptions>>()
                            .Get((string)key!);
            return new McpClientFactory(
                sp,
                options,
                sp.GetService<ILoggerFactory>());
        });

        // Convenience: the unnamed registration is also resolvable without a key.
        if (name == DefaultName) {
            services.TryAddSingleton(sp =>
                sp.GetRequiredKeyedService<IMcpClientFactory>(DefaultName));
        }

        return new McpClientBuilder(services, name);
    }
}


/// <summary>Provides extension methods for configuring an <see cref="McpClient"/> in the dependency injection container.</summary>
public static class McpClientBuilderExtensions
{
    /// <summary>Configures the <see cref="McpClient"/> to use a stdio transport with the specified command.</summary>
    public static IMcpClientBuilder WithStdioTransport(
        this IMcpClientBuilder builder,
        string command,
        Action<StdioClientTransportOptions>? configure) {
        builder.Services
            .AddOptions<McpClientRegistrationOptions>(builder.Name)
            .Configure(o => {
                var transportOptions = new StdioClientTransportOptions() {
                    Command = command
                };
                configure?.Invoke(transportOptions);
                o.TransportFactory = sp =>
                    new StdioClientTransport(transportOptions, sp.GetService<ILoggerFactory>());
            });
        return builder;
    }

    /// <summary>Configures the <see cref="McpClient"/> to use an HTTP transport with the specified endpoint.</summary>
    public static IMcpClientBuilder WithHttpTransport(
        this IMcpClientBuilder builder,
        Uri endpoint,
        Action<IServiceProvider, HttpClientTransportOptions>? configure) {
        builder.Services
            .AddOptions<McpClientRegistrationOptions>(builder.Name)
            .Configure<IServiceProvider>((o, sp) => {
                var transportOptions = new HttpClientTransportOptions() {
                    Endpoint = endpoint
                };
                configure?.Invoke(sp, transportOptions);
                o.TransportFactory = _ => new HttpClientTransport(transportOptions);
            });
        return builder;
    }

    /// <summary>Configures the <see cref="McpClient"/> to use an HTTP transport with the specified endpoint and a custom <see cref="HttpClient"/> factory.</summary>
    /// <remarks>Use this overload when the <see cref="HttpClient"/> carries a preconfigured handler pipeline (for example bearer token acquisition for machine-to-machine scenarios).</remarks>
    public static IMcpClientBuilder WithHttpTransport(
        this IMcpClientBuilder builder,
        Uri endpoint,
        Func<IServiceProvider, HttpClient> httpClientFactory,
        Action<IServiceProvider, HttpClientTransportOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        builder.Services
            .AddOptions<McpClientRegistrationOptions>(builder.Name)
            .Configure<IServiceProvider, ILoggerFactory>((o, sp, loggerFactory) => {
                var transportOptions = new HttpClientTransportOptions() {
                    Endpoint = endpoint,
                    TransportMode = HttpTransportMode.StreamableHttp
                };
                configure?.Invoke(sp, transportOptions);
                o.TransportFactory = provider => new HttpClientTransport(
                    transportOptions,
                    httpClientFactory(provider),
                    loggerFactory);
            });
        return builder;
    }

    /// <summary>Configures the <see cref="McpClient"/> to use a custom transport factory.</summary>
    public static IMcpClientBuilder WithTransport(
        this IMcpClientBuilder builder,
        Func<IServiceProvider, IClientTransport> factory) {
        builder.Services
            .AddOptions<McpClientRegistrationOptions>(builder.Name)
            .Configure(o => o.TransportFactory = factory);
        return builder;
    }

    /// <summary>Configures the <see cref="McpClient"/> to share its session across multiple instances.</summary>
    public static IMcpClientBuilder ShareSession(this IMcpClientBuilder builder, bool share = true) {
        builder.Services
            .AddOptions<McpClientRegistrationOptions>(builder.Name)
            .Configure(o => o.ShareSession = share);
        return builder;
    }
}

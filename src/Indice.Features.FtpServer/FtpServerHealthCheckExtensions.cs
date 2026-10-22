using FubarDev.FtpServer;
using FubarDev.FtpServer.DataConnection;
using Indice.Features.FtpServer.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the FTP server connection tracking and health checks.
/// </summary>
public static class FtpServerHealthCheckExtensions
{
    /// <summary>
    /// Enables tracking of the passive listeners and data connections of the FTP server, and registers the <see cref="IFtpServerCapacityProvider"/>
    /// that can be used to query the active connections and the remaining capacity (e.g. from a custom HTTP endpoint).
    /// </summary>
    /// <remarks>
    /// Must be called inside the <c>AddFtpServer(...)</c> configuration so that the default <see cref="IPasvListenerFactory"/> is already registered.
    /// </remarks>
    /// <param name="builder">The FTP server builder to configure.</param>
    /// <returns>The same instance of <see cref="IFtpServerBuilder"/> to allow for method chaining.</returns>
    public static IFtpServerBuilder EnableConnectionTracking(this IFtpServerBuilder builder) {
        var services = builder.Services;
        if (services.Any(x => x.ServiceType == typeof(ConnectionTrackingMarker))) {
            return builder;
        }
        services.AddSingleton<ConnectionTrackingMarker>();
        services.AddFtpServerCapacityServices();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFtpDataConnectionValidator, TrackingDataConnectionValidator>());
        DecoratePasvListenerFactory(services);
        return builder;
    }

    /// <summary>
    /// Adds a health check that reports the FTP server readiness, the active control/data connections and the remaining capacity.
    /// </summary>
    /// <remarks>
    /// For data connection metrics to be populated, <see cref="EnableConnectionTracking(IFtpServerBuilder)"/> must be called on the FTP server builder.
    /// </remarks>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to <c>ftp</c>.</param>
    /// <param name="configure">Optional configuration of the <see cref="FtpServerHealthCheckOptions"/>.</param>
    /// <param name="failureStatus">The <see cref="HealthStatus"/> that should be reported when the server is not ready. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="timeout">An optional <see cref="TimeSpan"/> representing the timeout of the check.</param>
    /// <returns>The <see cref="IHealthChecksBuilder"/> for further configuration.</returns>
    public static IHealthChecksBuilder AddFtpServer(
        this IHealthChecksBuilder builder,
        string name = "ftp",
        Action<FtpServerHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        builder.Services.AddFtpServerCapacityServices();
        if (configure is not null) {
            builder.Services.Configure(name, configure);
        }
        return builder.AddCheck<FtpServerHealthCheck>(name, failureStatus, tags ?? [], timeout);
    }

    private static IServiceCollection AddFtpServerCapacityServices(this IServiceCollection services) {
        services.AddOptions();
        services.TryAddSingleton<FtpServerConnectionTracker>();
        services.TryAddSingleton<IFtpServerCapacityProvider, FtpServerCapacityProvider>();
        return services;
    }

    private static void DecoratePasvListenerFactory(IServiceCollection services) {
        var descriptor = services.LastOrDefault(x => x.ServiceType == typeof(IPasvListenerFactory) && !x.IsKeyedService);
        Func<IServiceProvider, IPasvListenerFactory> innerFactory;
        var lifetime = ServiceLifetime.Singleton;
        if (descriptor is null) {
            innerFactory = sp => ActivatorUtilities.CreateInstance<PasvListenerFactory>(sp);
        } else {
            services.Remove(descriptor);
            lifetime = descriptor.Lifetime;
            innerFactory = descriptor switch {
                { ImplementationInstance: IPasvListenerFactory instance } => _ => instance,
                { ImplementationFactory: not null } => sp => (IPasvListenerFactory)descriptor.ImplementationFactory(sp),
                { ImplementationType: not null } => sp => (IPasvListenerFactory)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType),
                _ => throw new InvalidOperationException($"Unable to decorate the registered {nameof(IPasvListenerFactory)}.")
            };
        }
        services.Add(ServiceDescriptor.Describe(
            typeof(IPasvListenerFactory),
            sp => new TrackingPasvListenerFactory(innerFactory(sp), sp.GetRequiredService<FtpServerConnectionTracker>()),
            lifetime));
    }

    private sealed class ConnectionTrackingMarker { }
}

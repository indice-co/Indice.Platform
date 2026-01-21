using Azure.Core.Serialization;
using Indice.Serialization;
using Indice.Services;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering SignalR Proxy core services with an <see cref="IServiceCollection"/>.
/// </summary>
public static class IServiceCollectionSignalRExtensions
{
    /// <summary>
    /// Adds the core services required for SignalR Proxy functionality.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configureAction">An optional action to configure the SignalR service manager options.</param>
    /// <returns>The updated service collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Azure SignalR connection string is not configured.</exception>
    public static IServiceCollection AddSignalRProxyCoreServices(this IServiceCollection services, Action<ServiceManagerOptions>? configureAction = null) {
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(serviceManagerOptions => {
                configureAction?.Invoke(serviceManagerOptions);
                 if (string.IsNullOrEmpty(serviceManagerOptions.ConnectionString)) {
                     throw new InvalidOperationException("SignalR ConnectionString is not configured.");
                }
                serviceManagerOptions.ServiceTransportType = ServiceTransportType.Transient;
                serviceManagerOptions.UseJsonObjectSerializer(
                    new JsonObjectSerializer(JsonSerializerOptionDefaults.GetDefaultSettings()));
                
            })
            .BuildServiceManager();
        services.AddSingleton(serviceManager);
        services.TryAddSingleton<SignalRProxyHubContextStore>();
        services.TryAddTransient<ISignalRProxyNegotiatiationService, SignalRProxyNegotiatiationService>();
        services.TryAddTransient<ISignalRProxyBroadcastService, SignalRProxyBroadcastService>();
        services.AddDefaultPlatformEventService();
        return services;
    }
}

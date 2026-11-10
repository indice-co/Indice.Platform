using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/> for configuring real time inbox notifications.</summary>
public static class InboxNotifierServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IInboxNotifier"/> that pushes a personalized notification through SignalR (Azure SignalR service in serverless mode) 
    /// to the recipient every time a new inbox message is created.
    /// </summary>
    /// <remarks>
    /// Clients connect using the SignalR proxy endpoints (<c>AddSignalRProxy()</c> / <c>MapSignalRProxy()</c> from the <i>Indice.AspNetCore</i> package) 
    /// and must negotiate against the same hub name configured here. The SignalR connection string is read from <c>ConnectionStrings:SignalR</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configure the available options. Null to use defaults.</param>
    public static IServiceCollection AddMessageInboxSignalRNotifications(this IServiceCollection services, Action<MessageInboxNotificationOptions>? configure = null) {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(ISignalRProxyBroadcastService))) {
            services.AddSignalRProxyCoreServices();
        }
        var options = new MessageInboxNotificationOptions();
        configure?.Invoke(options);
        services.Configure<MessageInboxNotificationOptions>(x => {
            x.HubName = options.HubName;
            x.MethodName = options.MethodName;
            x.NotifyGlobalMessages = options.NotifyGlobalMessages;
        });
        services.Replace(ServiceDescriptor.Transient<IInboxNotifier, InboxNotifierSignalR>());
        return services;
    }

    /// <summary>Registers a custom <see cref="IInboxNotifier"/> implementation.</summary>
    /// <typeparam name="TInboxNotifier">The concrete type of <see cref="IInboxNotifier"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddMessageInboxNotifier<TInboxNotifier>(this IServiceCollection services) where TInboxNotifier : class, IInboxNotifier {
        services.Replace(ServiceDescriptor.Transient<IInboxNotifier, TInboxNotifier>());
        return services;
    }
}

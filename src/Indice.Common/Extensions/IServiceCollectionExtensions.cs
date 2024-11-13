using System.Reflection;
using Indice.Events;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on the <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Add a decorator pattern implementation.</summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The decorator.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddDecorator<TService, TDecorator>(this IServiceCollection services)
        where TService : class
        where TDecorator : class, TService {
        var serviceDescriptor = services.Where(x => x.ServiceType == typeof(TService)).LastOrDefault();
        if (serviceDescriptor is null) {
            services.AddTransient<TService, TDecorator>();
            return services;
        }
        if (serviceDescriptor.ImplementationType is not null) {
            services.TryAddTransient(serviceDescriptor.ImplementationType);
        }
        return services.AddTransient<TService, TDecorator>(serviceProvider => {
            var parameters = typeof(TDecorator).GetConstructors(BindingFlags.Public | BindingFlags.Instance).First().GetParameters();
            var arguments = parameters.Select(x => x.ParameterType.Equals(typeof(TService))
                ? serviceDescriptor.ImplementationFactory?.Invoke(serviceProvider) ?? serviceProvider.GetRequiredService(serviceDescriptor.ImplementationType!)
                : serviceProvider.GetService(x.ParameterType)).ToArray();
            return (TDecorator)Activator.CreateInstance(typeof(TDecorator), arguments)!;
        });
    }

    /// <summary>Adds a fugazi implementation of <see cref="IPushNotificationService"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddPushNotificationServiceNoop(this IServiceCollection services) {
        services.TryAddTransient<IPushNotificationServiceFactory, DefaultPushNotificationServiceFactory>();
        services.TryAddSingleton<IPushNotificationService, PushNotificationServiceNoop>();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="ILockManager"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddLockManagerNoop(this IServiceCollection services) {
        services.TryAddSingleton<ILockManager, LockManagerNoop>();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="ILockManager"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddEventDispatcherNoop(this IServiceCollection services) {
        services.TryAddTransient<IEventDispatcherFactory, DefaultEventDispatcherFactory>();
        services.TryAddSingleton<IEventDispatcher, EventDispatcherNoop>();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="IEmailService"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddEmailServiceNoop(this IServiceCollection services) {
        services.TryAddSingleton<IEmailService, EmailServiceNoop>();
        services.AddHtmlRenderingEngineNoop();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="IHtmlRenderingEngine"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddHtmlRenderingEngineNoop(this IServiceCollection services) {
        services.TryAddSingleton<IHtmlRenderingEngine, HtmlRenderingEngineNoop>();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="IFileService"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddFileServiceNoop(this IServiceCollection services) {
        services.TryAddSingleton<IFileService, FileServiceNoop>();
        return services;
    }

    /// <summary>Adds a fugazi implementation of <see cref="ISmsService"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddSmsServiceNoop(this IServiceCollection services) {
        services.TryAddTransient<ISmsServiceFactory, DefaultSmsServiceFactory>();
        services.TryAddSingleton<ISmsService, SmsServiceNoop>();
        return services;
    }

    /// <summary>Adds the default implementation of <see cref="IZoneInfoProvider"/>.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddZoneInfoProvider(this IServiceCollection services) {
        services.AddZoneInfoProvider<SystemZoneInfoProvider>();
        return services;
    }

    /// <summary>Adds a concrete implementation of <see cref="IZoneInfoProvider"/></summary>
    /// <typeparam name="TZoneInfoProvider">The implementation of <see cref="IZoneInfoProvider"/></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddZoneInfoProvider<TZoneInfoProvider>(this IServiceCollection services) where TZoneInfoProvider : IZoneInfoProvider {
        services.TryAddTransient(typeof(IZoneInfoProvider), typeof(TZoneInfoProvider));
        return services;
    }

    /// <summary>Adds the default implementation of <see cref="IPlatformEventService"/> which processes events synchronously as part of the request lifecycle.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddDefaultPlatformEventService(this IServiceCollection services) {
        services.TryAddTransient<IPlatformEventService, DefaultPlatformEventService>();
        return services;
    }

#if !NETSTANDARD2_1
    /// <summary>Adds the default implementation of <see cref="IPlatformEventService"/> which processes events αsynchronously on the background.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="config">Configuration action.</param>
    public static IServiceCollection AddBackgroundPlatformEventService(this IServiceCollection services, Action<BackgroundPlatformEventServiceQueueOptions>? config = null) {
        services.TryAddTransient<IPlatformEventService, BackgroundPlatformEventService>();
        services.TryAddSingleton<BackgroundPlatformEventServiceQueue>();
        services.Configure<BackgroundPlatformEventServiceQueueOptions>(options => config?.Invoke(options));
        services.AddHostedService<BackgroundPlatformEventHostedService>();
        return services;
    }
#endif
}

using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions on the <see cref="IServiceCollection"/>.</summary>
public static class IServiceCollectionExtensions
{
    /// <summary>Adds a fugazi implementation of <see cref="IPushNotificationService"/> that does nothing.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddPushNotificationServiceNoop(this IServiceCollection services) {
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
}

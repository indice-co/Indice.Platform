using Indice.Events;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.EntityFrameworkCore;
using Indice.Features.Identity.Server.ActivityLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Identity.Server.ActivityLog;

/// <summary>
/// Extension methods used to register the required services for managing user's sign log activity for IdentityServer.
/// </summary>
public static class ActivityLogsExtensions
{
    /// <summary>
    /// Registers the required services for managing user's sign log activity for IdentityServer.
    /// </summary>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static TBuilder AddActivityLogs<TBuilder>(this TBuilder builder, IConfiguration configuration, Action<ActivityLogIdentityOptions>? configure = null) where TBuilder : IIdentityServerBuilder {
        var options = new ActivityLogIdentityOptions(builder.Services, configuration);
        configure?.Invoke(options);
        builder.Services.AddActivityLogs(configuration, options.Configure);
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this ActivityLogIdentityOptions options, Action<IServiceProvider, DbContextOptionsBuilder> configure) {
        var services = options.Services;
        services.AddDbContext<ActivityLogDbContext>(configure);
        services.AddTransient<IActivityLogStore, ActivityLogStore>();
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this ActivityLogIdentityOptions options, Action<DbContextOptionsBuilder> configure) =>
        options.UseEntityFrameworkCoreStore((serviceProvider, builder) => configure(builder));

    /// <summary>
    /// Registers a platform event handler that listens to platform events and converts them to activity log entries using the specified converter factory.
    /// </summary>
    /// <typeparam name="TFactory">The type of the event converter factory.</typeparam>
    /// <param name="options">The activity log options.</param>
    /// <returns>.</returns>
    public static void ListenToPlatformEvents<TFactory>(this ActivityLogIdentityOptions options) where TFactory : class, IActivityLogFromEventConverter {
        options.Services.TryAddTransient<IActivityLogFromEventConverter, TFactory>();
        options.Services.AddTransient(typeof(IPlatformEventHandler<>), typeof(ActivityLogAdapterEventHandler<>));
    }
}

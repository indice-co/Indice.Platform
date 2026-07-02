using System.Reflection;
using Indice.Events;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.EntityFrameworkCore;
using Indice.Features.ActivityLogs.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods used to register the required services for managing activity logs for IdentityServer.</summary>
public static class ActivityLogFeatureExtensions
{
    /// <summary>
    /// Registers and configures necessary services for the activity log feature to work.
    /// </summary>
    /// <param name="services">The service collection to add the activity log services to.</param>
    /// <param name="configuration">The configuration to use for the activity log services.</param>
    /// <param name="configureAction">An optional action to configure the activity log options.</param>
    /// <returns></returns>
    public static IActivityLogBuilder AddActivityLogs(this IServiceCollection services,IConfiguration configuration, Action<ActivityLogOptions>? configureAction = null) {
        var options = new ActivityLogOptions();
        configureAction?.Invoke(options);
        var activityLogBuilder = new ActivityLogBuilder(services, configuration);
        if (configureAction != null) {
            services.Configure(configureAction);
        }
        if (!options.Enable) {
            return activityLogBuilder;
        }
        // 3. Core Services
        services.AddTransient<IActivityLogPublisher, ActivityLogPublisher>();
        services.AddHostedService<PersistLogsHostedService>();
        services.AddTransient<ActivityLogEntryEnricherAggregator>();
        services.AddSingleton<ActivityLogEntryQueue>();
        services.AddGeoIPResolver();

        services.AddDefaultEnrichers([.. options.ExcludedEnrichers]);
        services.AddDefaultFilters();
        if (options.Cleanup.Enable) {
            services.AddHostedService<LogCleanupHostedService>();
        }
        services.TryAddSingleton<IActivityLogStore, ActivityLogStoreNoop>();

        return activityLogBuilder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static IActivityLogBuilder UseEntityFrameworkCoreStore(this IActivityLogBuilder builder, Action<IServiceProvider, DbContextOptionsBuilder> configure) {
        var services = builder.Services;
        services.AddDbContext<ActivityLogDbContext>(configure);
        services.AddTransient<IActivityLogStore, ActivityLogStore>();
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="builder">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static IActivityLogBuilder UseEntityFrameworkCoreStore(this IActivityLogBuilder builder, Action<DbContextOptionsBuilder> configure) =>
        builder.UseEntityFrameworkCoreStore((serviceProvider, builder) => configure(builder));

    /// <summary>Adds a custom enricher.</summary>
    /// <typeparam name="TEnricher"></typeparam>
    /// <param name="builder">The host application builder.</param>
    public static IActivityLogBuilder AddEnricher<TEnricher>(this IActivityLogBuilder builder) where TEnricher : class, IActivityLogEntryEnricher {
        builder.Services.AddActivityLogEnricher<TEnricher>();
        return builder;
    }

    private static IServiceCollection AddActivityLogEnricher<TEnricher>(this IServiceCollection services) where TEnricher : class, IActivityLogEntryEnricher {
        services.AddActivityLogEnricher(typeof(TEnricher));
        return services;
    }

    private static IServiceCollection AddActivityLogEnricher(this IServiceCollection services, Type type) {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IActivityLogEntryEnricher), type)
        );
        return services;
    }

    private static IServiceCollection AddDefaultEnrichers(this IServiceCollection services, params Type[] excludedTypes) {
        var enrichers = AssemblyInternalExtensions.GetClassesAssignableFrom<IActivityLogEntryEnricher>(Assembly.GetExecutingAssembly()).Except(excludedTypes);
        foreach (var enricher in enrichers) {
            services.AddActivityLogEnricher(enricher);
        }
        return services;
    }

    private static IServiceCollection AddDefaultFilters(this IServiceCollection services) {
        var filters = AssemblyInternalExtensions.GetClassesAssignableFrom<IActivityLogEntryFilter>(Assembly.GetExecutingAssembly());
        foreach (var filter in filters) {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(typeof(IActivityLogEntryFilter), filter)
            );
        }
        return services;
    }

    /// <summary>Adds a custom filter.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="type">The type of the filter to add.</param>
    public static IActivityLogBuilder AddFilter(this IActivityLogBuilder builder, Type type) {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IActivityLogEntryFilter), type)
        );
        return builder;
    }

    /// <summary>
    /// Adds a custom filter of type <typeparamref name="TFilter"/> to the activity log builder.
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IActivityLogBuilder AddFilter<TFilter>(this IActivityLogBuilder builder) where TFilter : class, IActivityLogEntryFilter {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IActivityLogEntryFilter, TFilter>()
        );
        return builder;
    }

    /// <summary>
    /// Configures the activity store by ensuring the associated database is created.
    /// </summary>
    /// <remarks>This method ensures that the database for storing activity logs is created if it does not
    /// already exist. It uses a scoped service to access the <see cref="ActivityLogDbContext"/> and calls EnsureCreated.</remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance used to configure the application.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance passed as the <paramref name="app"/> parameter, allowing for method chaining.</returns>
    public static IApplicationBuilder ActivityStoreSetup(this IApplicationBuilder app) {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<ActivityLogDbContext>();
        dbContext.Database.EnsureCreated();
        return app;
    }

    /// <summary>
    /// Configures the activity log feature to listen to platform events and create activity log entries based on those events.
    /// </summary>
    /// <param name="builder"> The host application builder.</param>
    /// <returns> The same <see cref="IActivityLogBuilder"/> instance passed as the <paramref name="builder"/> parameter, allowing for method chaining.</returns>
    public static IActivityLogBuilder ListenToPlatformEvents(this IActivityLogBuilder builder)
        => ListenToPlatformEvents<DefaultActivityLogFromEventConverter>(builder);

    /// <summary>
    /// Configures the activity log builder to listen for platform events using the specified event factory.
    /// </summary>
    /// <remarks>This method registers the specified event factory and the corresponding event handler for
    /// platform events, enabling the activity log to respond to those events.</remarks>
    /// <typeparam name="TFactory">The type of the event factory that implements the IActivityLogFromEventFactory interface.</typeparam>
    /// <param name="builder">The activity log builder instance to configure for platform event handling.</param>
    /// <returns>The configured instance of IActivityLogBuilder, allowing for method chaining.</returns>
    public static IActivityLogBuilder ListenToPlatformEvents<TFactory>(this IActivityLogBuilder builder) where TFactory : class, IActivityLogFromEventConverter {
        builder.Services.TryAddTransient<IActivityLogFromEventConverter, TFactory>();
        builder.Services.AddTransient(typeof(IPlatformEventHandler<>), typeof(ActivityLogAdapterEventHandler<>));
        return builder;
    }

}
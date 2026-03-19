using System.Reflection;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.EntityFrameworkCore;
using Indice.Features.ActivityLogs.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods used to register the required services for managing user's sign log activity for IdentityServer.</summary>
public static class ActivityLogFeatureExtensions
{
    /// <summary>
    /// Registers and configures necessary services for the activity log feature to work.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IActivityLogBuilder AddActivityLogs(this IHostApplicationBuilder builder, Action<ActivityLogOptions> configure) {
        var options = new ActivityLogOptions();
        configure.Invoke(options);
        var activityLogBuilder = new ActivityLogBuilder(builder.Services, builder.Configuration);
        builder.Services.Configure<ActivityLogOptions>(configure);
        if (!options.Enable) {
            return activityLogBuilder;
        }

        // 3. Core Services
        builder.Services.AddTransient<IActivityEventPublisher, ActivityLogEventPublisher>();
        builder.Services.AddHostedService<PersistLogsHostedService>();
        builder.Services.AddTransient<ActivityLogEntryEnricherAggregator>();
        builder.Services.AddSingleton<ActivityLogEntryQueue>();
        builder.Services.AddGeoIPResolver();

        builder.Services.AddDefaultEnrichers([.. options.ExcludedEnrichers]);
        builder.Services.AddDefaultFilters();
        if (options.Cleanup.Enable) {
            builder.Services.AddHostedService<LogCleanupHostedService>();
        }
        builder.Services.TryAddSingleton<IActivityLogStore, ActivityLogStoreNoop>();

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
        services.AddTransient(typeof(IActivityLogEntryEnricher), type);
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
            services.AddTransient(typeof(IActivityLogEntryFilter), filter);
        }
        return services;
    }

    private static IActivityLogBuilder AddFilter(this IActivityLogBuilder builder, Type type) {
        builder.Services.AddTransient(typeof(IActivityLogEntryFilter), type);
        return builder;
    }

    
    /// <summary>
    /// Configures the activity store by ensuring the associated database is created.
    /// </summary>
    /// <remarks>This method ensures that the database for storing activity logs is created if it does not
    /// already exist. It uses a scoped service to access the <see cref="ActivityLogDbContext"/> and calls EnsureCreated.</remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance used to configure the application.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance passed as the <paramref name="app"/> parameter, allowing for
    /// method chaining.</returns>
    public static IApplicationBuilder ActivityStoreSetup(this IApplicationBuilder app) {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetService<ActivityLogDbContext>();
        dbContext.Database.EnsureCreated();
        return app;
    }
}
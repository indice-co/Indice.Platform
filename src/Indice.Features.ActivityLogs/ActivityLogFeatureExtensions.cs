using System.Reflection;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.EntityFrameworkCore;
using Indice.Features.ActivityLogs.EventHandlers;
using Indice.Features.ActivityLogs.Hosting;
using Indice.Features.ActivityLogs.ImpossibleTravel;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods used to register the required services for managing user's sign log activity for IdentityServer.</summary>
public static class ActivityLogFeatureExtensions
{
    /// <summary>Registers the <see cref="ActivityLogEventSink"/> implementation to the IdentityServer infrastructure.</summary>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the activity log feature.</param>
    public static TBuilder AddActivityLogs<TBuilder>(this TBuilder builder, Action<ActivityLogOptions> configure) where TBuilder : IIdentityServerBuilder =>
        builder.AddActivityLogs<TBuilder, User>(configure);

    /// <summary>Registers the <see cref="ActivityLogEventSink"/> implementation to the IdentityServer infrastructure.</summary>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the activity log feature.</param>
    public static TBuilder AddActivityLogs<TBuilder, TUser>(this TBuilder builder, Action<ActivityLogOptions> configure)
        where TBuilder : IIdentityServerBuilder
        where TUser : User {
        var services = builder.Services;
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var resolvedOptions = new ActivityLogOptions(builder.Services, configuration) {
            Enable = configuration.GetActivityLogsEnabled() ?? ActivityLogOptions.DEFAULT_ENABLE
        };
        resolvedOptions.ImpossibleTravel.Guard = configuration.GetImpossibleTravelEnabled() ?? ImpossibleTravelOptions.DEFAULT_IMPOSSIBLE_TRAVEL_GUARD;
        configure.Invoke(resolvedOptions);
        // Add IdentityServer sink that captures required activity events.
        if (!resolvedOptions.Enable) {
            return builder;
        }
        builder.AddEventSink<ActivityLogEventSink>();
        services.AddSingleton<IHostedService, PersistLogsHostedService>();
        // Configure options.
        services.Configure<ActivityLogOptions>(options => {
            options.AnonymizePersonalData = resolvedOptions.AnonymizePersonalData;
            options.ApiPrefix = resolvedOptions.ApiPrefix;
            options.ApiScope = resolvedOptions.ApiScope;
            options.Cleanup.BatchSize = resolvedOptions.Cleanup.BatchSize;
            options.Cleanup.Enable = resolvedOptions.Cleanup.Enable;
            options.Cleanup.IntervalSeconds = resolvedOptions.Cleanup.IntervalSeconds;
            options.Cleanup.RetentionDays = resolvedOptions.Cleanup.RetentionDays;
            options.DatabaseSchema = resolvedOptions.DatabaseSchema;
            options.Enable = resolvedOptions.Enable;
            options.QueueChannelCapacity = resolvedOptions.QueueChannelCapacity;
            options.ImpossibleTravel.Guard = resolvedOptions.ImpossibleTravel.Guard;
            options.ImpossibleTravel.AcceptableSpeed = resolvedOptions.ImpossibleTravel.AcceptableSpeed;
            options.ImpossibleTravel.FlowType = resolvedOptions.ImpossibleTravel.FlowType;
            options.Events.TokenEvents = resolvedOptions.Events.TokenEvents;
            options.Events.PasswordEvents = resolvedOptions.Events.PasswordEvents;
            options.DequeueBatchSize = resolvedOptions.DequeueBatchSize;
            options.DequeueTimeoutInMilliseconds = resolvedOptions.DequeueTimeoutInMilliseconds;
        });
        // Add built-in enrichers & filters for the log entry model.
        services.AddDefaultEnrichers([.. resolvedOptions.ExcludedEnrichers]);
        services.AddDefaultFilters();
        services.AddTransient<ActivityLogEntryEnricherAggregator>();
        services.AddSingleton<ActivityLogEntryQueue>();
        services.AddGeoIPResolver();
        // Enable feature management for this module.
        services.AddFeatureManagement(configuration.GetSection(IdentityServerFeatures.Section));
        // Add a default implementation in case one is not specified. Avoids DI errors.
        services.TryAddSingleton<IActivityLogStore, ActivityLogStoreNoop>();
        // if enabled, register log cleanup hosted (background) service.
        if (resolvedOptions.Cleanup.Enable) {
            services.AddSingleton<IHostedService, LogCleanupHostedService>();
        }
        if (resolvedOptions.ImpossibleTravel.Guard) {
            // Configure impossible travel feature.
            services.AddScoped<IImpossibleTravelDetector<TUser>, ImpossibleTravelDetector<TUser>>();
            var serviceDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IActivityGuard<TUser>));
            if (serviceDescriptor is not null) {
                builder.Services.Remove(serviceDescriptor);
            }
        }
        services.AddPlatformEventHandler<AccountLockedEvent, AccountLockedEventHandler>();
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this ActivityLogOptions options, Action<IServiceProvider, DbContextOptionsBuilder> configure) {
        var services = options.Services;
        services.AddDbContext<ActivityLogDbContext>(configure);
        services.AddTransient<IActivityLogStore, ActivityLogStoreEntityFrameworkCore>();
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this ActivityLogOptions options, Action<DbContextOptionsBuilder> configure) =>
        options.UseEntityFrameworkCoreStore((serviceProvider, builder) => configure(builder));

    /// <summary>Adds a custom enricher.</summary>
    /// <typeparam name="TEnricher"></typeparam>
    /// <param name="ActivityLogOptions">Options for configuring the IdentityServer activity logs mechanism.</param>
    public static void AddEnricher<TEnricher>(this ActivityLogOptions ActivityLogOptions) where TEnricher : class, IActivityLogEntryEnricher =>
        ActivityLogOptions.Services.AddActivityLogEnricher<TEnricher>();

    /// <summary>Removes an existing enricher.</summary>
    /// <typeparam name="TEnricher"></typeparam>
    /// <param name="ActivityLogOptions">Options for configuring the IdentityServer activity logs mechanism.</param>
    public static void RemoveEnricher<TEnricher>(this ActivityLogOptions ActivityLogOptions) where TEnricher : class, IActivityLogEntryEnricher =>
        ActivityLogOptions.ExcludedEnrichers.Add(typeof(TEnricher));

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

    
    /// <summary>
    /// Configures the sign-in store by ensuring the associated database is created.
    /// </summary>
    /// <remarks>This method ensures that the database for storing sign-in logs is created if it does not
    /// already exist. It uses a scoped service to access the <see cref="ActivityLogDbContext"/> and calls EnsureCreated.</remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance used to configure the application.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance passed as the <paramref name="app"/> parameter, allowing for
    /// method chaining.</returns>
    public static IApplicationBuilder ActivityStoreSetup(this IApplicationBuilder app) {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetService<ActivityLogDbContext>();
        dbContext?.Database.EnsureCreated();
        return app;
    }
}
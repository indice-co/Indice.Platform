using System.Reflection;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.SignInLogs;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Enrichers;
using Indice.Features.Identity.SignInLogs.EntityFrameworkCore;
using Indice.Features.Identity.SignInLogs.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods used to register the required services for managing user's sign log activity for IdentityServer.</summary>
public static class SignInLogFeatureExtensions
{
    /// <summary>Registers the <see cref="SignInLogEventSink"/> implementation to the IdentityServer infrastructure.</summary>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the sign in log feature.</param>
    public static TBuilder AddSignInLogs<TBuilder>(this TBuilder builder, Action<SignInLogOptions> configure) where TBuilder : IIdentityServerBuilder {
        var services = builder.Services;
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var resolvedOptions = new SignInLogOptions(builder.Services, configuration);
        configure.Invoke(resolvedOptions);
        // Add IdentityServer sink that captures required sign in events.
        if (!resolvedOptions.Enable) {
            return builder;
        }
        builder.AddEventSink<SignInLogEventSink>();
        services.AddSingleton<IHostedService, PersistLogsHostedService>();
        // Configure options.
        services.Configure<SignInLogOptions>(options => {
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
        });
        // Add built-in enrichers & filters for the log entry model.
        services.AddDefaultEnrichers(resolvedOptions.ExcludedEnrichers.ToArray());
        services.AddDefaultFilters();
        services.AddTransient<SignInLogEntryEnricherAggregator>();
        services.AddSingleton<SignInLogEntryQueue>();
        // Enable feature management for this module.
        services.AddFeatureManagement(configuration.GetSection(IdentityServerFeatures.Section));
        // Add a default implementation in case one is not specified. Avoids DI errors.
        services.TryAddSingleton<ISignInLogStore, SignInLogStoreNoop>();
        // if enabled, register log cleanup hosted (background) service.
        if (resolvedOptions.Cleanup.Enable) {
            services.AddSingleton<IHostedService, LogCleanupHostedService>();
        }
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this SignInLogOptions options, Action<IServiceProvider, DbContextOptionsBuilder> configure) {
        var services = options.Services;
        services.AddDbContext<SignInLogDbContext>(configure);
        services.AddTransient<ISignInLogStore, SignInLogStoreEntityFrameworkCore>();
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this SignInLogOptions options, Action<DbContextOptionsBuilder> configure) =>
        options.UseEntityFrameworkCoreStore((serviceProvider, builder) => configure(builder));

    /// <summary>Adds a custom enricher.</summary>
    /// <typeparam name="TEnricher"></typeparam>
    /// <param name="signInLogOptions">Options for configuring the IdentityServer sign in logs mechanism.</param>
    public static void AddEnricher<TEnricher>(this SignInLogOptions signInLogOptions) where TEnricher : class, ISignInLogEntryEnricher =>
        signInLogOptions.Services.AddSignInLogEnricher<TEnricher>();

    /// <summary>Removes an existing enricher.</summary>
    /// <typeparam name="TEnricher"></typeparam>
    /// <param name="signInLogOptions">Options for configuring the IdentityServer sign in logs mechanism.</param>
    public static void RemoveEnricher<TEnricher>(this SignInLogOptions signInLogOptions) where TEnricher : class, ISignInLogEntryEnricher =>
        signInLogOptions.ExcludedEnrichers.Add(typeof(TEnricher));

    private static IServiceCollection AddSignInLogEnricher<TEnricher>(this IServiceCollection services) where TEnricher : class, ISignInLogEntryEnricher {
        services.AddSignInLogEnricher(typeof(TEnricher));
        return services;
    }

    private static IServiceCollection AddSignInLogEnricher(this IServiceCollection services, Type type) {
        services.AddTransient(typeof(ISignInLogEntryEnricher), type);
        return services;
    }

    private static IServiceCollection AddDefaultEnrichers(this IServiceCollection services, params Type[] excludedTypes) {
        var enrichers = AssemblyInternalExtensions.GetClassesAssignableFrom<ISignInLogEntryEnricher>(Assembly.GetExecutingAssembly()).Except(excludedTypes);
        foreach (var enricher in enrichers) {
            services.AddSignInLogEnricher(enricher);
        }
        return services;
    }

    private static IServiceCollection AddDefaultFilters(this IServiceCollection services) {
        var filters = AssemblyInternalExtensions.GetClassesAssignableFrom<ISignInLogEntryFilter>(Assembly.GetExecutingAssembly());
        foreach (var filter in filters) {
            services.AddTransient(typeof(ISignInLogEntryFilter), filter);
        }
        return services;
    }
}
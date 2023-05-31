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
        configure(resolvedOptions);
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
        // Add IdentityServer sink that captures required sign in events.
        if (resolvedOptions.Enable) {
            builder.AddEventSink<SignInLogEventSink>();
            services.AddSingleton<IHostedService, PersistLogsHostedService>();
        }
        // Add built-in enrichers & filters for the log entry model.
        services.AddDefaultEnrichers();
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

    /// <summary>Uses Azure table storage as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    public static void UseAzureTableStorageStore(this SignInLogOptions options) {
        throw new NotImplementedException();
    }

    /// <summary>Adds all enrichers contained in an assembly enrichers.</summary>
    /// <typeparam name="TEnricher">The type that is contained in the assembly to scan.</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddSignInLogEnricher<TEnricher>(this IServiceCollection services) where TEnricher : class, ISignInLogEntryEnricher {
        services.AddTransient<ISignInLogEntryEnricher, TEnricher>();
        return services;
    }

    /// <summary>Adds all enrichers contained in an assembly enrichers.</summary>
    /// <typeparam name="T">The type that is contained in the assembly to scan</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    public static IServiceCollection AddSignInLogEnrichersFromAssembly<T>(this IServiceCollection services) {
        var enrichers = AssemblyInternalExtensions.GetClassesAssignableFrom<ISignInLogEntryEnricher>(typeof(T).Assembly);
        foreach (var enricher in enrichers) {
            services.AddTransient(typeof(ISignInLogEntryEnricher), enricher);
        }
        return services;
    }

    private static IServiceCollection AddDefaultEnrichers(this IServiceCollection services) {
        var enrichers = AssemblyInternalExtensions.GetClassesAssignableFrom<ISignInLogEntryEnricher>(Assembly.GetExecutingAssembly());
        foreach (var enricher in enrichers) {
            services.AddTransient(typeof(ISignInLogEntryEnricher), enricher);
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

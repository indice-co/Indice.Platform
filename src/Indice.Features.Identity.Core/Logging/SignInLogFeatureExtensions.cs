using System.Reflection;
using Indice.Features.Identity.Core.Logging;
using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods used to register the required services for managing user's sign log activity for IdentityServer.</summary>
public static class SignInLogFeatureExtensions
{
    /// <summary>Registers the <see cref="SignInLogEventSink"/> implementation to the IdentityServer infrastructure.</summary>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configure">Configure action for the </param>
    public static IIdentityServerBuilder AddUserSignInLogs(this IIdentityServerBuilder builder, Action<SignInLogOptions> configure) {
        var services = builder.Services;
        var options = new SignInLogOptions {
            Services = builder.Services
        };
        configure(options);
        builder.AddEventSink<SignInLogEventSink>();
        services.TryAddSingleton<ISignInLogService, SignInLogServiceNoop>();
        services.AddDefaultEnrichers();
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="configure">Provides a simple API surface for configuring <see cref="DbContextOptions" />.</param>
    public static void UseEntityFrameworkCoreStore(this SignInLogOptions options, Action<IServiceProvider, DbContextOptionsBuilder> configure) {
        var services = options.Services;
        services.AddTransient<ISignInLogService, SignInLogServiceEntityFrameworkCore>();
        services.AddDbContext<SignInLogDbContext>(configure);
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

    private static IServiceCollection AddDefaultEnrichers(this IServiceCollection services) {
        var enrichers = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsClass && typeof(ISignInLogEntryEnricher).IsAssignableFrom(type));
        foreach (var enricher in enrichers) {
            services.AddTransient(typeof(ISignInLogEntryEnricher), enricher);
        }
        return services;
    }
}

using Indice.Features.Identity.Core;
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
    public static IIdentityServerBuilder AddUserSignInLogs(this IIdentityServerBuilder builder, Action<AuditEventSinkOptions> configure) {
        var services = builder.Services;
        builder.AddEventSink<SignInLogEventSink>();
        var options = new AuditEventSinkOptions {
            Services = builder.Services
        };
        configure(options);
        services.TryAddSingleton<ISignInLogService, SignInLogServiceNoop>();
        return builder;
    }

    /// <summary>Uses Entity Framework Core as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    /// <param name="builder"></param>
    public static void UseEntityFrameworkCoreStore(this AuditEventSinkOptions options, Action<IServiceProvider, DbContextOptionsBuilder> builder) {
        var services = options.Services;
        services.AddTransient<ISignInLogService, SignInLogServiceEntityFrameworkCore>();
        services.AddDbContext<SignInLogDbContext>(builder);
    }

    /// <summary>Uses Azure table storage as a persistence store.</summary>
    /// <param name="options">Options for configuring the IdentityServer audit mechanism.</param>
    public static void UseAzureTableStorageStore(this AuditEventSinkOptions options) {
        throw new NotImplementedException();
    }
}

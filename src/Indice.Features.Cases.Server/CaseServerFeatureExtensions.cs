using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Cases.Server;

/// <summary>
/// Adds all services needed to configure Case management server.
/// </summary>
public static class CaseServerFeatureExtensions
{
    /// <summary>Adds the case server dependencies.</summary>
    /// <param name="builder">Specifies the <see cref="IHostApplicationBuilder"/> to configure</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostApplicationBuilder AddCaseServer(
        this IHostApplicationBuilder builder,
        Action<CaseServerOptions>? setupAction = null
    ) {
        // Configure options given by the consumer.
        var serverOptions = new CaseServerOptions();
        setupAction?.Invoke(serverOptions);
        builder.Services.Configure<CaseServerOptions>(options => {
            options.PathPrefix = serverOptions.PathPrefix;
            options.DatabaseSchema = serverOptions.DatabaseSchema;
            options.RequiredScope = serverOptions.RequiredScope;
            options.UserClaimType = serverOptions.UserClaimType;
            options.GroupIdClaimType = serverOptions.GroupIdClaimType;
            options.GroupName = serverOptions.GroupName;
            options.ConfigureLimitUpload = serverOptions.ConfigureLimitUpload;
        });
        builder.Services.AddCasesCore(options => {
            options.DatabaseSchema = serverOptions.DatabaseSchema;
            options.RequiredScope = serverOptions.RequiredScope;
            options.UserClaimType = serverOptions.UserClaimType;
            options.GroupIdClaimType = serverOptions.GroupIdClaimType;
        });
        builder.Services.AddLimitUpload(serverOptions.ConfigureLimitUpload);
        return builder;
    }
}


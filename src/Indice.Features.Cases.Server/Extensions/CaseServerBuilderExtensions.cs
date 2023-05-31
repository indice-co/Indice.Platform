using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds all services needed to configure Case management server.
/// </summary>
public static class CaseServerBuilderExtensions
{
    /// <summary>Adds the case server dependencies.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="environment">Provides information about the web hosting environment an application is running in.</param>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The <see cref="ICaseServerBuilder"/>.</returns>
    public static ICaseServerBuilder AddCaseServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        Action<CaseServerOptions>? setupAction = null
    )
    {
        return new CaseServerBuilder(services, configuration, environment);
    }

    /// <summary>Add the Backoffice configuration. This includes both apis and services to run backoffice operations</summary>
    /// <param name="builder">The builder</param>
    /// <returns>The builder</returns>
    public static ICaseServerBuilder AddWorkflow(ICaseServerBuilder builder)
    {
        return builder;
    }

    /// <summary>Add the Backoffice configuration. This includes both apis and services to run backoffice operations</summary>
    /// <param name="builder">The builder</param>
    /// <returns>The builder</returns>
    public static ICaseServerBuilder AddCaseManagerEndpoints(ICaseServerBuilder builder) {
        return builder;
    }


    /// <summary>My cases endpoints. This includes customer facing apis and services</summary>
    /// <param name="builder">The builder</param>
    /// <returns>The builder</returns>
    public static ICaseServerBuilder AddMyCasesEndpoints(ICaseServerBuilder builder)
    {
        return builder;
    }
}

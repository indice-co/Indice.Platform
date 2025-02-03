using Indice.Functions.Builder;
using Microsoft.Azure.Functions.Worker.Builder;

namespace Microsoft.Extensions.Hosting;
/// <summary>
/// Extension methods to configure resiliency and telemetry settings.
/// Services include OpenTelemetry, HttpClient default resiliency, Healthcheks and their respective /health, /alive endpoints
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Adds webserive defaults for ConfigureOpenTelemetry, AddDefaultHealthChecks, ConfigureHttpClientDefaults
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostBuilder ConfigureFunctionsDefaults(this IHostBuilder builder) {
        builder.ConfigureServices((context, services) => {
            services.AddWorkerServiceOpenTelemetry(context);
            services.AddFunctionHealthChecks();
        });
        return builder;
    }

}

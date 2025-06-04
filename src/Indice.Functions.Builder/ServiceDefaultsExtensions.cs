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
    public static FunctionsApplicationBuilder ConfigureFunctionsDefaults(this FunctionsApplicationBuilder builder) {
        builder.Services.AddWorkerServiceOpenTelemetry(builder.Environment);
        builder.Services.AddFunctionHealthChecks();
        return builder;
    }

}

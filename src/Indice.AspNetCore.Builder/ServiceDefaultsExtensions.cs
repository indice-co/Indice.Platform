using System.Reflection;
using System.Text.Json;
using System.Text;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
    public static IHostApplicationBuilder AddWebServiceDefaults(this IHostApplicationBuilder builder) {
        builder.AddOpenTelemetryDefaults();

        builder.AddDefaultHealthChecks();

        builder.Services.ConfigureHttpClientDefaults(http => {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    /// <summary>
    /// Adds worker defaults for ConfigureOpenTelemetry, ConfigureHttpClientDefaults
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostApplicationBuilder AddWorkerServiceDefaults(this IHostApplicationBuilder builder) {
        builder.AddOpenTelemetryDefaults();

        builder.Services.ConfigureHttpClientDefaults(http => {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();
        });

        return builder;
    }

    /// <summary>
    /// Configure OpenTelemetry
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostApplicationBuilder AddOpenTelemetryDefaults(this IHostApplicationBuilder builder) {
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing => {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
                if (builder.Environment.IsDevelopment()) {
                    tracing.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
                }
                tracing.ConfigureResource(resource => resource
                //https://rehansaeed.com/optimally-configuring-open-telemetry-tracing-for-asp-net-core/
                // Uncomment the following line to enable fixed metadata for all traces.
                // Go to definition on ResourceSemanticConventions to see available settings.
                /*
                 .AddAttributes(new Dictionary<string, object>() {
                    ["service.name"] = "my-service",
                    ["service.namespace"] = "my-namespace",
                    ["service.instance.id"] = "my-instance"
                })
                */
                .AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceNamespace: GetServiceNamespace(),
                    serviceVersion: Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
                    autoGenerateServiceInstanceId: true)
                // The resource detectors will record the following metadata based on where your application is running:
                // ContainerDetector: container.id.
                .AddContainerDetector()
                /* Makes use of environment variables OTEL_RESOURCE_ATTRIBUTES and OTEL_SERVICE_NAME
                 * OTEL_RESOURCE_ATTRIBUTES: "service.name=my-service,service.namespace=my-namespace,service.instance.id=my-instance"
                 * OTEL_SERVICE_NAME: "my-service"
                 */
                .AddEnvironmentVariableDetector()
                );

            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }
    private static string GetServiceNamespace() {
        var namespaceArray = Assembly.GetEntryAssembly()?.GetName()?.Name?.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (namespaceArray == null || namespaceArray.Length == 0)
            return "indice";
        return namespaceArray[0].ToLower();
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder) {
        // Uncomment the following lines to enable the OLTP telemetry exporter.
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter) {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

        // enables the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        else if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])) {
            builder.Services.AddOpenTelemetry()
                    .UseAzureMonitor();
        }

        return builder;
    }

    /// <summary>
    /// Adds default healthcheck endpoints
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for further configuration.</returns>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder) {
        // these are registered always but used in product`
        builder.Services.AddRequestTimeouts(
            configure: static timeouts =>
                timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

        builder.Services.AddOutputCache(
            configureOptions: static caching =>
                caching.AddPolicy("HealthChecks",
                build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));


        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps the default endpoints for /alive and /health
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure</param>
    /// <returns>The <see cref="WebApplication"/> for further configuration</returns>
    public static WebApplication MapHealthCheckDefaults(this WebApplication app) {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment()) {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/details", new HealthCheckOptions {
                ResponseWriter = WriteResponse
            });
            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions {
                Predicate = r => r.Tags.Contains("live")
            });

            // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
            // app.MapPrometheusScrapingEndpoint();
        } else {
            var healthChecks = app.MapGroup("");
            healthChecks.CacheOutput("HealthChecks")
                        .WithRequestTimeout("HealthChecks");
            // All health checks must pass for app to be
            // considered ready to accept traffic after starting
            healthChecks.MapHealthChecks("/health");
            healthChecks.MapHealthChecks("/health/details", new HealthCheckOptions {
                ResponseWriter = WriteResponse
            });
            // Only health checks tagged with the "live" tag
            // must pass for app to be considered alive
            healthChecks.MapHealthChecks("/alive", new() {
                Predicate = static r => r.Tags.Contains("live")
            });
        }

        return app;
    }
    private static Task WriteResponse(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options)) {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var healthReportEntry in healthReport.Entries) {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status",
                    healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description",
                    healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");

                foreach (var item in healthReportEntry.Value.Data) {
                    jsonWriter.WritePropertyName(item.Key);

                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value?.GetType() ?? typeof(object));
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}

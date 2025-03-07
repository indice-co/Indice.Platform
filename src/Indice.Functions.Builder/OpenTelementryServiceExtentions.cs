using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Reflection;
using OpenTelemetry;

namespace Indice.Functions.Builder;

/// <summary>
/// Provides extension methods for setting up OpenTelemetry in Azure Functions.
/// </summary>
public static class OpenTelementryServiceExtentions
{
    /// <summary>
    /// Use OpenTelemetry with Azure Functions
    /// </summary>
    /// <remarks> article: <a href="https://learn.microsoft.com/en-us/azure/azure-functions/opentelemetry-howto?tabs=app-insights&amp;pivots=programming-language-csharp">OpenTelemetry How to</a> </remarks>
    public static IServiceCollection AddWorkerServiceOpenTelemetry(this IServiceCollection services, IHostEnvironment environment) {

        var optelBuilder = services.AddOpenTelemetry()
                                    .UseFunctionsWorkerDefaults()
                                    //When the host is configured to use OpenTelemetry, only logs and traces are exported. Host metrics aren't currently exported.
                                    /*.AddMetrics()*/
                                    .AddTracing(environment);
        // Uncomment the following lines to enable the OLTP telemetry exporter.
        var useOtlpExporter = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
        if (useOtlpExporter) {
            optelBuilder.UseOtlpExporter();
        }
        // enables the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"))) {
            optelBuilder.UseAzureMonitor();
        }
        return services;
    }

    /* Uncomment when we find out what to do with metrics
    private static OpenTelemetryBuilder AddMetrics(this OpenTelemetryBuilder builder) {
        builder.WithMetrics(metrics => {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        });
        return builder;
    }
    */

    /// <summary>
    /// Add tracing to the OpenTelemetry builder
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder.</param>
    /// <param name="environment">Host environemt</param>
    /// <returns>The updated OpenTelemetry builder.</returns>
    private static OpenTelemetryBuilder AddTracing(this OpenTelemetryBuilder builder, IHostEnvironment environment) {
        builder.WithTracing(tracing => {
            tracing.AddAspNetCoreInstrumentation()
                // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                //.AddGrpcClientInstrumentation()
                .AddHttpClientInstrumentation();
            //add package OpenTelemetry.Instrumentation.EntityFrameworkCore
            //.AddEntityFrameworkCoreInstrumntation();

            tracing.ConfigureResource(resource => resource .AddService(
                serviceName: environment.ApplicationName,
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
        return builder;
    }
    /// <summary>
    /// Get the service namespace from the entry assembly
    /// </summary>
    /// <returns></returns>
    private static string GetServiceNamespace() {
        var namespaceArray = Assembly.GetEntryAssembly()?.GetName()?.Name?.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        if (namespaceArray == null || namespaceArray.Length == 0)
            return "indice";
        return namespaceArray[0].ToLower();
    }

}

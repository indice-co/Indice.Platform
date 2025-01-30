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
    /// <remarks> article: https://learn.microsoft.com/en-us/azure/azure-functions/opentelemetry-howto?tabs=app-insights&pivots=programming-language-csharp </remarks>
    public static IServiceCollection AddWorkerServiceOpenTelemetry(this IServiceCollection services, HostBuilderContext context) {
        // Uncomment the following lines to enable the OLTP telemetry exporter.
        var useOtlpExporter = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
        if (useOtlpExporter) {
            services.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults()
                    //When the host is configured to use OpenTelemetry, only logs and traces are exported. Host metrics aren't currently exported.
                    /*.AddMetrics()*/
                    .AddTracing(context)
                    .UseOtlpExporter();
        }
        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());
        // enables the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"))) {
            services.AddOpenTelemetry()
                    .UseFunctionsWorkerDefaults()
                    .UseAzureMonitor()
                    //When the host is configured to use OpenTelemetry, only logs and traces are exported. Host metrics aren't currently exported.
                    //.AddMetrics()
                    .AddTracing(context);
        }
        return services;
    }

    private static OpenTelemetryBuilder AddMetrics(this OpenTelemetryBuilder builder) {
        builder.WithMetrics(metrics => {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        });
        return builder;
    }
    /// <summary>
    /// Add tracing to the OpenTelemetry builder
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder.</param>
    /// <param name="context">The host builder context.</param>
    /// <returns>The updated OpenTelemetry builder.</returns>
    private static OpenTelemetryBuilder AddTracing(this OpenTelemetryBuilder builder, HostBuilderContext context) {
        builder.WithTracing(tracing => {
            tracing.AddAspNetCoreInstrumentation()
                // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                //.AddGrpcClientInstrumentation()
                .AddHttpClientInstrumentation();
            //add package OpenTelemetry.Instrumentation.EntityFrameworkCore
            //.AddEntityFrameworkCoreInstrumntation();

            //Not applcable since debug support is not available
            //if (context.HostingEnvironment.IsDevelopment())
            //{
            //    tracing.AddConsoleExporter(options => options.Targets = ConsoleExporterOutputTargets.Debug);
            //}
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
                serviceName: context.HostingEnvironment.ApplicationName,
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

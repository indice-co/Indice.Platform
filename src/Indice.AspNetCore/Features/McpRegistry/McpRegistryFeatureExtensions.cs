using Indice.AspNetCore.Features.McpRegistry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.Hosting;

/// <summary>Service registration extensions for the MCP registry feature.</summary>
public static class McpRegistryFeatureExtensions
{
    /// <summary>
    /// Adds the MCP registry services. The registry data are bound from the <paramref name="sectionName"/> section of the application configuration
    /// and are reloaded whenever the configuration changes.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="sectionName">The configuration section name. Defaults to <see cref="McpRegistryOptions.SectionName"/>.</param>
    /// <param name="configureAction">An optional action to further configure the <see cref="McpRegistryOptions"/>.</param>
    public static IHostApplicationBuilder AddMcpRegistry(this IHostApplicationBuilder builder, string sectionName = McpRegistryOptions.SectionName, Action<McpRegistryOptions>? configureAction = null) {
        builder.Services.AddMcpRegistry(builder.Configuration, sectionName, configureAction);
        return builder;
    }

    /// <summary>
    /// Adds the MCP registry services. The registry data are bound from the <paramref name="sectionName"/> section of the <paramref name="configuration"/>
    /// and are reloaded whenever the configuration changes.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The configuration section name. Defaults to <see cref="McpRegistryOptions.SectionName"/>.</param>
    /// <param name="configureAction">An optional action to further configure the <see cref="McpRegistryOptions"/>.</param>
    public static IServiceCollection AddMcpRegistry(this IServiceCollection services, IConfiguration configuration, string sectionName = McpRegistryOptions.SectionName, Action<McpRegistryOptions>? configureAction = null) {
        ArgumentNullException.ThrowIfNull(configuration);
        var optionsBuilder = services.AddOptions<McpRegistryOptions>().Bind(configuration.GetSection(sectionName));
        if (configureAction is not null) {
            optionsBuilder.Configure(configureAction);
        }
        services.AddSingleton<IValidateOptions<McpRegistryOptions>, McpRegistryOptionsValidator>();
        optionsBuilder.ValidateOnStart();
        services.AddCors(cors => cors.AddPolicy(McpRegistryOptions.CorsPolicyName, policy => policy
            .AllowAnyOrigin()
            .WithMethods(HttpMethods.Get, HttpMethods.Options)
            .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType)));
        return services;
    }
}

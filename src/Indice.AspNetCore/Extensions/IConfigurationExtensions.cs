using System.Collections.Generic;
using Indice.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions to configure the <see cref="IConfiguration"/> of an ASP.NET Core application.
    /// </summary>
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Indicates whether to register mock implementations of services in the DI.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the General:MockServices option in appsettings.json file.</remarks>
        public static bool UseMockServices(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.MockServices));

        /// <summary>
        /// Indicates whether to redirect http to https.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the General:UseHttpsRedirection option in appsettings.json file. When true you can register <see cref="HttpsPolicyBuilderExtensions.UseHttpsRedirection(IApplicationBuilder)"/> middleware.</remarks>
        public static bool UseHttpsRedirection(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.UseHttpsRedirection));

        /// <summary>
        /// Indicates whether to enable the Swagger UI.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the General:SwaggerUI option in appsettings.json file.</remarks>
        public static bool EnableSwaggerUi(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.EnableSwagger));

        /// <summary>
        /// A list of endpoints used throughout the application.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>Endpoints defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
        /// <remarks>Checks for the General:Endpoints option in appsettings.json file.</remarks>
        public static Dictionary<string, string> GetEndpoints(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<Dictionary<string, string>>(nameof(GeneralSettings.Endpoints));

        /// <summary>
        /// Gets the endpoint value using the specified key.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="key">The key to search for.</param>
        /// <returns>The endpoint under the specified key. Endpoints are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
        /// <remarks>Checks for the General:Endpoints option in appsettings.json file.</remarks>
        /// <exception cref="KeyNotFoundException">Throws a <see cref="KeyNotFoundException"/> if the specified key is not found.</exception>
        public static string GetEndpoint(this IConfiguration configuration, string key) => GetEndpoints(configuration)[key];

        /// <summary>
        /// Tries to get the endpoint value using the specified key.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="key">The key to search for.</param>
        /// <returns>The endpoint under the specified key if the key exists, otherwise null. Endpoints are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
        public static string TryGetEndpoint(this IConfiguration configuration, string key) => GetEndpoints(configuration).TryGetValue(key, out var endpoint) ? endpoint : default;

        /// <summary>
        /// Indicates whether to enable HSTS (HTTP Strict Transport Security).
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the General:HstsEnabled option in appsettings.json file. When true you can register <see cref="HstsBuilderExtensions.UseHsts(IApplicationBuilder)"/> middleware.</remarks>
        public static bool HstsEnabled(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.HstsEnabled));

        /// <summary>
        /// Indicates whether a proxy is enabled.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the Proxy:Enabled option in appsettings.json file.</remarks>
        public static bool ProxyEnabled(this IConfiguration configuration) => configuration.GetSection(ProxyOptions.Name).GetValue<bool>(nameof(ProxyOptions.Enabled));

        /// <summary>
        /// Gets the proxy's IP address.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>The proxy's IP address.</returns>
        /// <remarks>Checks for the Proxy:Ip option in appsettings.json file.</remarks>
        public static string GetProxyIp(this IConfiguration configuration) => configuration.GetSection(ProxyOptions.Name).GetValue<string>(nameof(ProxyOptions.Ip));

        /// <summary>
        /// Indicates whether to stop the worker host, running the background tasks.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <returns>True if specified flag is set to true, otherwise false.</returns>
        /// <remarks>Checks for the General:StopWorkerHost option in appsettings.json file.</remarks>
        public static bool StopWorkerHost(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>("StopWorkerHost");
    }
}

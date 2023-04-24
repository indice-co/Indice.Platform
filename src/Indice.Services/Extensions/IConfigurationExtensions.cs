using Indice.Configuration;
using Indice.Types;

namespace Microsoft.Extensions.Configuration;

/// <summary>Extensions to configure the <see cref="IConfiguration"/> of an ASP.NET Core application.</summary>
public static class IConfigurationExtensions
{
    /// <summary>Indicates whether to register mock implementations of services in the DI.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:MockServices</strong> option in appsettings.json file.</remarks>
    public static bool UseMockServices(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.MockServices));

    /// <summary>Indicates whether to redirect HTTP to HTTPS.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:UseHttpsRedirection</strong> option in appsettings.json file. When true you can register HttpsPolicyBuilderExtensions.UseHttpsRedirection(IApplicationBuilder) middleware.</remarks>
    public static bool UseHttpsRedirection(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.UseHttpsRedirection));

    /// <summary>A flag that indicates whether to redirect the setting that is defined in <see cref="GeneralSettings.Host"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:UseRedirectToHost</strong> option in appsettings.json file.</remarks>
    public static bool UseRedirectToHost(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.UseRedirectToHost));

    /// <summary>Indicates whether to enable the Swagger UI.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:EnableSwagger</strong> option in appsettings.json file.</remarks>
    public static bool EnableSwaggerUi(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.EnableSwagger));

    /// <summary>A list of endpoints used throughout the application.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>Endpoints defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    /// <remarks>Checks for the <strong>General:Endpoints</strong> option in appsettings.json file.</remarks>
    public static Dictionary<string, string> GetEndpoints(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}:{nameof(GeneralSettings.Endpoints)}").Get<Dictionary<string, string>>();

    /// <summary>Gets the endpoint value using the specified key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>The endpoint under the specified key. Endpoints are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    /// <remarks>Checks for the <strong>General:Endpoints</strong> option in appsettings.json file.</remarks>
    /// <exception cref="KeyNotFoundException">Throws a <see cref="KeyNotFoundException"/> if the specified key is not found.</exception>
    public static string GetEndpoint(this IConfiguration configuration, string key) => GetEndpoints(configuration)[key];

    /// <summary>Tries to get the endpoint value using the specified key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>The endpoint under the specified key if the key exists, otherwise null. Endpoints are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    public static string TryGetEndpoint(this IConfiguration configuration, string key) => GetEndpoints(configuration).TryGetValue(key, out var endpoint) ? endpoint : default;

    /// <summary>Indicates whether to enable HSTS (HTTP Strict Transport Security).</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:HstsEnabled</strong> option in appsettings.json file. When true you can register HstsBuilderExtensions.UseHsts(IApplicationBuilder) middleware.</remarks>
    public static bool HstsEnabled(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>(nameof(GeneralSettings.HstsEnabled));

    /// <summary>Indicates whether a proxy is enabled.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>Proxy:Enabled</strong> option in appsettings.json file.</remarks>
    public static bool ProxyEnabled(this IConfiguration configuration) => configuration.GetSection(ProxyOptions.Name).GetValue<bool>(nameof(ProxyOptions.Enabled));

    /// <summary>Gets the proxy's IP address.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The proxy's IP address.</returns>
    /// <remarks>Checks for the <strong>Proxy:Ip</strong> option in appsettings.json file.</remarks>
    public static string GetProxyIp(this IConfiguration configuration) => configuration.GetSection(ProxyOptions.Name).GetValue<string>(nameof(ProxyOptions.Ip));

    /// <summary>Indicates whether to stop the worker host, running the background tasks.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>General:WorkerHostDisabled</strong> option in appsettings.json file.</remarks>
    public static bool WorkerHostDisabled(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<bool>("WorkerHostDisabled") || configuration.GetValue<bool>("WorkerHostDisabled");

    /// <summary>Indicates whether developer Totp featured is enabled. </summary>
    /// <param name="configuration"></param>
    /// <returns>True if specified flag is set to true, otherwise false.</returns>
    /// <remarks>Checks for the <strong>Totp:EnableDeveloperTotp</strong> or <strong>EnableDeveloperTotp</strong> option in appsettings.json file.</remarks>
    public static bool DeveloperTotpEnabled(this IConfiguration configuration) => configuration.GetSection("Totp").GetValue<bool>("EnableDeveloperTotp") || configuration.GetValue<bool>("EnableDeveloperTotp");

    /// <summary>Gets the Application Insights instrumentation key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>Checks for the <strong>ApplicationInsights:InstrumentationKey</strong> option in appsettings.json file.</returns>
    public static string GetInstrumentationKey(this IConfiguration configuration) => configuration.GetSection("ApplicationInsights").GetValue<string>("InstrumentationKey");

    /// <summary>Gets the Application Insights Connection String.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>Checks for the <strong>ApplicationInsights:ConnectionString</strong> option in appsettings.json file.</returns>
    public static string GetApplicationInsightsConnectionString(this IConfiguration configuration) => configuration.GetSection("ApplicationInsights").GetValue<string>("ConnectionString") ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

    /// <summary>A string that represents the default host name binding for this web application <see cref="GeneralSettings.Host"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>Example can be https://www.example.com</returns>
    /// <remarks>Checks for the <strong>General:Host</strong> option in appsettings.json file.</remarks>
    public static string GetHost(this IConfiguration configuration) => configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.Host))?.TrimEnd('/');

    /// <summary>A string that represents the default host name binding for the identity provider (aka authority) for this application <see cref="GeneralSettings.Authority"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="tryInternal">Try to retrieve the internal network base address URL for the IdentityServer. Fallsback to Authority if not set. Defaults to false.</param>
    /// <returns>Example can be https://idp.example.com</returns>
    /// <remarks>Checks either the <strong>General:AuthorityInternal</strong> or <strong>General:Authority</strong> option in appsettings.json file. Depends up on the <paramref name="tryInternal"/> parameter.</remarks>
    public static string GetAuthority(this IConfiguration configuration, bool tryInternal = false) => tryInternal 
        ? configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.AuthorityInternal))?.TrimEnd('/') ?? configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.Authority))?.TrimEnd('/')
        : configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.Authority))?.TrimEnd('/');

    /// <summary>A string that represents the default host name binding for the identity provider (aka authority) for this application <see cref="GeneralSettings.Authority"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="tryInternal">Try to retrieve the internal network base address URL for the IdentityServer. Fallsback to Authority if not set. Defaults to false.</param>
    /// <returns>Example can be https://idp.example.com/.well-known/openid-configuration</returns>
    /// <remarks>Checks either the <strong>General:AuthorityInternal</strong> or <strong>General:Authority</strong> option in appsettings.json file. Depends up on the <paramref name="tryInternal"/> parameter.</remarks>
    public static string GetAuthorityMetadata(this IConfiguration configuration, bool tryInternal = false) => $"{GetAuthority(configuration, tryInternal)}/.well-known/openid-configuration";

    /// <summary>Get an object class that represents all the configuration for an Api.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns><see cref="ApiSettings"/></returns>
    /// <remarks>Checks for the <strong>General:Api</strong> option in appsettings.json file and binds it to the <see cref="ApiSettings"/> class.</remarks>
    public static ApiSettings GetApiSettings(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}:{nameof(GeneralSettings.Api)}").Get<ApiSettings>();

    /// <summary>Get an object class that represents all the configuration under the <strong>General</strong> configuration section.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>A snapshot of the current <see cref="GeneralSettings"/></returns>
    /// <remarks>Checks for the <strong>General</strong> option in appsettings.json file and binds it to the <see cref="GeneralSettings"/> class.</remarks>
    public static GeneralSettings GetGeneralSettings(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}").Get<GeneralSettings>();
    
    /// <summary>A string that represents the api resource scope.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The api resource name. Or in other words the api base scope</returns>
    /// <remarks>Checks for the <strong>General:Api:ResourceName</strong> option in appsettings.json file.</remarks>
    public static string GetApiResourceName(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}:{nameof(GeneralSettings.Api)}").GetValue<string>(nameof(ApiSettings.ResourceName));

    /// <summary>A string that represents the api friendly name. This is used as the api display name in the swagger ui.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The api display name</returns>
    /// <remarks>Checks for the <strong>General:Api:FriendlyName</strong> option in appsettings.json file.</remarks>
    public static string GetApiFriendlyName(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}:{nameof(GeneralSettings.Api)}").GetValue<string>(nameof(ApiSettings.FriendlyName));

    /// <summary>A list of symmetric keys/secrets used by the api.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>Secrets defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    /// <remarks>Checks for the <strong>General:Api:Secrets</strong> option in appsettings.json file.</remarks>
    public static Dictionary<string, string> GetApiSecrets(this IConfiguration configuration) => configuration.GetSection($"{GeneralSettings.Name}:{nameof(GeneralSettings.Api)}:{nameof(ApiSettings.Secrets)}").Get<Dictionary<string, string>>();
    
    /// <summary>Gets the api secret value using the specified key.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="key">The key to search for.</param>
    /// <returns>The api secret under the specified key. Api Secrets are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    /// <remarks>Checks for the <strong>General:Api:Secrets</strong> option in appsettings.json file.</remarks>
    /// <exception cref="KeyNotFoundException">Throws a <see cref="KeyNotFoundException"/> if the specified key is not found.</exception>
    public static string GetApiSecret(this IConfiguration configuration, string key) => GetApiSecrets(configuration)[key];

    /// <summary>Tries to get the signalR connection string only if valid.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="connectionString">Outputs the connection string if valid</param>
    /// <returns>The true if a valid connection string is found.</returns>
    /// <remarks>the name will be searched under the <strong>ConnenctionStrings:SignalRService</strong> option in appsettings.json file.</remarks>
    public static bool TryGetSignalRConnectionString(this IConfiguration configuration, out ConnectionString connectionString) =>
        TryGetSignalRConnectionString(configuration, "SignalRService", out connectionString);

    /// <summary>Tries to get the signalR connection string only if valid.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="connectionStringName">The name of the connection string to search for.</param>
    /// <param name="connectionString">Outputs the connection string if valid</param>
    /// <returns>The true if a valid connection string is found.</returns>
    /// <remarks>the name will be searched under the <strong>ConnenctionStrings:connectionStringName</strong> option in appsettings.json file.</remarks>
    public static bool TryGetSignalRConnectionString(this IConfiguration configuration, string connectionStringName, out ConnectionString connectionString) {
        connectionString = null;
        var signalRConnectionString = configuration.GetConnectionString(connectionStringName);
        try {
            connectionString = new ConnectionString(signalRConnectionString);
            if (connectionString.ContainsKey("Endpoint")) {
                return true;
            }
        } catch {
            return false;
        }
        return false;
    }

    /// <summary>Tries to get the applicationInsights connection string only if valid.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="connectionString">Outputs the connection string if valid</param>
    /// <returns>The api secret under the specified key. Api Secrets are defined in appssettings.json as a <see cref="Dictionary{String, String}"/>.</returns>
    /// <remarks>the name will be searched under the <strong>ApplicationInsights:ConnectionString</strong> option in appsettings.json file.</remarks>
    public static bool TryGetApplicationInsightsConnectionString(this IConfiguration configuration, out ConnectionString connectionString) {
        connectionString = null;
        var signalRConnectionString = configuration.GetApplicationInsightsConnectionString();
        try {
            connectionString = new ConnectionString(signalRConnectionString);
            if (connectionString.ContainsKey("IngestionEndpoint")) {
                return true;
            }
        } catch {
            return false;
        }
        return false;
    }
}

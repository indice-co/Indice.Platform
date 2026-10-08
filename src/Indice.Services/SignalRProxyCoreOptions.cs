using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Services;

/// <summary>
/// Configuration options for SignalR proxy core services.
/// </summary>
public class SignalRProxyCoreOptions
{
    /// <summary>
    /// The name of the connection string used to retrieve the SignalR connection string from configuration.
    /// </summary>
    public const string ConnectionStringName = "SignalR";
    
    /// <summary>
    /// Gets or sets the connection string for Azure SignalR Service.
    /// </summary>
    public string ConnectionString { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the current hosting environment.
    /// </summary>
    public string EnvironmentName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the current application name.
    /// </summary>
    /// <remarks>Will be used as a prefix for all SignalR hub names.</remarks>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hub names should be automatically prefixed with the environment name.
    /// </summary>
    public bool AutoPrefixWithEnvironmentName { get; set; }
    
    /// <summary>
    /// Gets or sets an optional action to configure the <see cref="ServiceManagerOptions"/> for the SignalR service manager.
    /// </summary>
    public Action<ServiceManagerOptions>? ConfigureServiceManager { get; set; }
}

/// <summary>
/// Provides configuration and post-configuration for <see cref="SignalRProxyCoreOptions"/>.
/// Automatically retrieves the SignalR connection string from configuration and sets the environment name.
/// </summary>
public class SignalRProxyCoreConfigureOptions : IConfigureOptions<SignalRProxyCoreOptions>
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRProxyCoreConfigureOptions"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The hosting environment information.</param>
    public SignalRProxyCoreConfigureOptions(IConfiguration configuration, IHostEnvironment environment) {
        _configuration = configuration;
        _environment = environment;
    }
    
    /// <summary>
    /// Configures the <see cref="SignalRProxyCoreOptions"/> by retrieving the connection string and environment name.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    public void Configure(SignalRProxyCoreOptions options) {
        options.ConnectionString = _configuration.GetConnectionString(SignalRProxyCoreOptions.ConnectionStringName)!;
        options.EnvironmentName = _environment.EnvironmentName;
    }
}

using Indice.Services;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Provides additional configurability for SignalR endpoints.
/// </summary>
public class SignalRProxyOptions
{
    private IServiceCollection? _services;

    /// <summary>The configuration section name for SignalR proxy options.</summary>
    public const string SectionName = "SignalRProxy";
    /// <summary>The authentication scheme used to secure the endpoints.</summary>
    public List<string> NegotiateAuthenticationSchemes { get; set; } = [];
    /// <summary>The endpoint route pattern for the SignalR endpoints.</summary>
    public string EndpointRoutePattern { get; set; } = "/";
    /// <summary>Optional group name for the endpoints.</summary>
    /// <remarks>If provided, the endpoints will be grouped under this name in Swagger/OpenAPI documentation.</remarks>
    public string? GroupName { get; set; } = "signalr";
    /// <summary>Required scope to access the endpoints.</summary>
    public string RequiredScope { get; set; } = null!;
    /// <summary>Decides whether to enable swagger/openapi documentation for the endpoint</summary>
    public bool ExcludeFromDescription { get; set; }
    /// <summary>List of allowed Hubs</summary>
    /// <remarks>If empty, all hubs are allowed.</remarks>
    public List<string> AllowedHubs { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether the environment name should be used as a prefix for SignalR hub names.
    /// </summary>
    /// <remarks>When enabled, the environment name (such as Development, Staging, or Production) is prepended
    /// to the hub name. This can help isolate SignalR traffic between different deployment environments and prevent
    /// cross-environment communication issues.</remarks>
    public bool UseEnvironmentNameAsHubPrefix { get; set; }
    /// <summary>Gets or sets the ApplicationName which will be prefixed to each hub name</summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets an optional action to configure the <see cref="ServiceManagerOptions"/> for the SignalR service manager.
    /// </summary>
    public Action<ServiceManagerOptions>? ConfigureServiceManager { get; set; }

    /// <summary>Gets or sets the service collection for dependency injection.</summary>
    /// <remarks>This property is set during service registration and should not be modified directly.</remarks>
    internal IServiceCollection Services
    {
        get => _services ?? throw new InvalidOperationException("Services property has not been initialized. Ensure AddSignalRProxy() has been called.");
        set => _services = value;
    }

    /// <summary>
    /// Registers a custom user ID resolver implementation.
    /// </summary>
    /// <typeparam name="TResolver">The type of the user ID resolver implementation.</typeparam>
    /// <returns>The current <see cref="SignalRProxyOptions"/> instance for method chaining.</returns>
    public SignalRProxyOptions AddUserIdResolver<TResolver>() where TResolver : class, ISignalRProxyUserIdResolver
    {
        Services.Replace(ServiceDescriptor.Singleton<ISignalRProxyUserIdResolver, TResolver>());
        return this;
    }

    /// <summary>
    /// Registers a custom group name validator implementation.
    /// </summary>
    /// <typeparam name="TValidator">The type of the group name validator implementation.</typeparam>
    /// <returns>The current <see cref="SignalRProxyOptions"/> instance for method chaining.</returns>
    public SignalRProxyOptions AddGroupNameValidator<TValidator>() where TValidator : class, ISignalRProxyGroupNameValidator
    {
        Services.Replace(ServiceDescriptor.Singleton<ISignalRProxyGroupNameValidator, TValidator>());
        return this;
    }
}

/// <summary>
/// Post-configures <see cref="SignalRProxyCoreOptions"/> by synchronizing configuration values from <see cref="SignalRProxyOptions"/>.
/// </summary>
/// <remarks>
/// This class is part of the options pattern and is executed after the initial configuration to ensure that
/// core options are properly aligned with the proxy options, specifically transferring the environment name prefix setting.
/// </remarks>
internal class PostConfigureSignalRProxyCoreOptions : IPostConfigureOptions<SignalRProxyCoreOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostConfigureSignalRProxyCoreOptions"/> class.
    /// </summary>
    /// <param name="apiOptions">The SignalR proxy options containing the configuration values to transfer.</param>
    public PostConfigureSignalRProxyCoreOptions(IOptions<SignalRProxyOptions> apiOptions) {
        ApiOptions = apiOptions;
    }

    /// <summary>
    /// Gets the SignalR proxy options used for post-configuration.
    /// </summary>
    public IOptions<SignalRProxyOptions> ApiOptions { get; }

    /// <summary>
    /// Post-configures the SignalR proxy core options by transferring configuration values.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    /// <remarks>
    /// This method synchronizes the <see cref="SignalRProxyOptions.UseEnvironmentNameAsHubPrefix"/> value
    /// to the <see cref="SignalRProxyCoreOptions.AutoPrefixWithEnvironmentName"/> property.
    /// </remarks>
    public void PostConfigure(string? name, SignalRProxyCoreOptions options) {
        options.AutoPrefixWithEnvironmentName = ApiOptions.Value.UseEnvironmentNameAsHubPrefix;
        options.ApplicationName = ApiOptions.Value.ApplicationName;
        options.ConfigureServiceManager = ApiOptions.Value.ConfigureServiceManager;
    }
}
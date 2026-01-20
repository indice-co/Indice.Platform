using System.Security.Claims;
using Indice.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
    /// <summary>List of Claims types to auto-populate Groups if available on the current claim principal</summary>
    public List<string> ClaimTypesForAutoGroups { get; set; } = [];

    /// <summary>List of Claims types to auto-populate Groups if available on the current claim principal</summary>
    /// <remarks>Defaults to <c>x => $"{x.Type}|{x.Value}"</c></remarks>
    public SignalRClaimTypeToGroupNameTransformer ClaimTypeToGroupName { get; set; } = x => $"{x.Type}|{x.Value}";
    /// <summary>List of allowed Hubs</summary>
    public List<string> AllowedHubs { get; set; } = [];

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
        Services.AddSingleton<ISignalRProxyUserIdResolver, TResolver>();
        return this;
    }

    /// <summary>
    /// Registers a custom group name validator implementation.
    /// </summary>
    /// <typeparam name="TValidator">The type of the group name validator implementation.</typeparam>
    /// <returns>The current <see cref="SignalRProxyOptions"/> instance for method chaining.</returns>
    public SignalRProxyOptions AddGroupNameValidator<TValidator>() where TValidator : class, ISignalRProxyGroupNameValidator
    {
        Services.AddSingleton<ISignalRProxyGroupNameValidator, TValidator>();
        return this;
    }
}

/// <summary>
/// A delegate to transform a Claim to a SignalR Group Name
/// </summary>
/// <param name="claim">The claim to transform.</param>
/// <returns>The transformed group name.</returns>
public delegate string SignalRClaimTypeToGroupNameTransformer(Claim claim);
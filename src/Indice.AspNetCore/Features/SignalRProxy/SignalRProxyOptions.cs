using System.Security.Claims;
using Indice.Extensions;

namespace Indice.AspNetCore.Features.SignalRProxy;

/// <summary>
/// Provides additional configurability for SignalR endpoints.
/// </summary>
public class SignalRProxyOptions
{
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
    /// <summary>List of Groups that anonymous users can join directly</summary>
    public List<string> AnonymousAllowedGroups { get; set; } = [];
    /// <summary>List of Claims types to auto-populate Groups if available on the current claim principal</summary>
    public List<string> ClaimTypesForAutoGroups { get; set; } = [];

    /// <summary>List of Claims types to auto-populate Groups if available on the current claim principal</summary>
    /// <remarks>Defaults to <c>x => $"{x.Type}|{x.Value}"</c></remarks>
    public SignalRClaimTypeToGroupNameTransformer ClaimTypeToGroupName { get; set; } = x => $"{x.Type}|{x.Value}";
    /// <summary>List of allowed Hubs</summary>
    public List<string> AllowedHubs { get; set; } = [];
}

/// <summary>
/// A delegate to transform a Claim to a SignalR Group Name
/// </summary>
/// <param name="claim">The claim to transform.</param>
/// <returns>The transformed group name.</returns>
public delegate string SignalRClaimTypeToGroupNameTransformer(Claim claim);
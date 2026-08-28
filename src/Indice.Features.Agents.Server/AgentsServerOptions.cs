
using Indice.Features.Agents.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Agents.Server;

/// <summary>
/// Agents server options
/// </summary>
public class AgentsServerOptions 
{
    /// <summary>The path prefix for the endpoints registered</summary>
    public PathString PathPrefix { get; set; } = "/";
    /// <summary>Endpoints group name</summary>
    /// <remarks>Default value is "agents".</remarks>
    public string GroupName { get; set; } = "agents";
    /// <summary>Chat endpoints security requirement.</summary>
    /// <remarks>Default value is "chat".</remarks>
    public string ChatRequiredScope { get; set; } = "chat";
    /// <summary>Ingest endpoints security requirement.</summary>
    /// <remarks>Default value is "ingest".</remarks>
    public string IngestRequiredScope { get; set; } = "ingest";
    /// <summary>
    /// Configure Agents core options 
    /// </summary>
    public Action<AgentsOptions>? ConfigureAgents { get; set; }
    /// <summary>Allows unauthenticated (anonymous) callers to create a chat session through <c>POST /my/chats</c> and <c>POST /my/chats/stream</c>.</summary>
    /// <remarks>
    /// When enabled, an ephemeral guest access token is acquired from the identity provider (through the <c>urn:indice:guest</c> grant configured in <see cref="GuestToken"/>)
    /// and returned with the create response, so the guest can access the rest of the (protected) chat endpoints.
    /// Anonymous creation is a potential abuse vector; it is strongly recommended to add rate limiting on these endpoints. Defaults to <see langword="false"/>.
    /// </remarks>
    public bool AllowAnonymousChatCreation { get; set; }
    /// <summary>Backchannel token client settings used to mint guest access tokens when <see cref="AllowAnonymousChatCreation"/> is enabled.</summary>
    public GuestTokenOptions GuestToken { get; set; } = new();
}

/// <summary>Backchannel token client settings for acquiring ephemeral guest access tokens from the identity provider.</summary>
public class GuestTokenOptions
{
    /// <summary>The default guest grant type.</summary>
    public const string DefaultGrantType = "urn:indice:guest";
    /// <summary>The base address of the identity provider (authority).</summary>
    public string? Authority { get; set; }
    /// <summary>Overrides the token endpoint. When not set, <c>{Authority}/connect/token</c> is used.</summary>
    public string? TokenEndpoint { get; set; }
    /// <summary>The client id used to authenticate against the token endpoint.</summary>
    public string? ClientId { get; set; }
    /// <summary>The client secret used to authenticate against the token endpoint.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The scope(s) to request. When not set, defaults to <see cref="AgentsServerOptions.ChatRequiredScope"/>.</summary>
    public string? Scope { get; set; } = "chat offline_access identity";
    /// <summary>The grant type to use. Defaults to <c>urn:indice:guest</c>.</summary>
    public string GrantType { get; set; } = DefaultGrantType;
}

using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Features.McpRegistry;

/// <summary>
/// Configuration options for the MCP registry feature. The registry follows the v0.1 MCP registry specification
/// so that it can be consumed by GitHub Copilot (VS Code, Visual Studio, JetBrains, Copilot CLI etc.).
/// </summary>
/// <remarks>
/// Typically bound from the <c>McpRegistry</c> configuration section:
/// <code>
/// "McpRegistry": {
///   "Servers": [
///     {
///       "Name": "io.github.my-org/weather",
///       "Description": "Weather forecasts",
///       "Version": "1.0.0",
///       "Remotes": [ { "Type": "streamable-http", "Url": "https://example.com/mcp" } ]
///     }
///   ]
/// }
/// </code>
/// </remarks>
public class McpRegistryOptions
{
    /// <summary>The default configuration section name.</summary>
    public const string SectionName = "McpRegistry";
    /// <summary>The name of the CORS policy applied to the registry endpoints.</summary>
    public const string CorsPolicyName = "McpRegistry";
    /// <summary>Path prefix under which the <c>v0.1/servers</c> endpoints are mapped. Defaults to <c>/</c>.</summary>
    public string PathPrefix { get; set; } = "/";
    /// <summary>Optional group name for the endpoints in the OpenAPI documentation.</summary>
    public string? GroupName { get; set; }
    /// <summary>Excludes the endpoints from the OpenAPI documentation.</summary>
    public bool ExcludeFromDescription { get; set; }
    /// <summary>
    /// Applies the <see cref="CorsPolicyName"/> CORS policy required by GitHub Copilot to the endpoints. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>Requires the CORS middleware in the pipeline (<c>app.UseCors()</c>).</remarks>
    public bool EnableCors { get; set; } = true;
    /// <summary>
    /// Optional authorization policy name to protect the endpoints. When empty the endpoints allow anonymous access.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }
    /// <summary>Default page size of the list endpoint when the <c>limit</c> parameter is not provided. Defaults to 30.</summary>
    public int DefaultPageSize { get; set; } = 30;
    /// <summary>Maximum allowed page size of the list endpoint. Defaults to 100.</summary>
    public int MaxPageSize { get; set; } = 100;
    /// <summary>The MCP servers (one entry per server version) exposed by the registry.</summary>
    public List<McpServerEntry> Servers { get; set; } = [];
}

/// <summary>Lifecycle status of a server version in the registry.</summary>
public enum McpServerStatus
{
    /// <summary>The server version is active.</summary>
    Active = 0,
    /// <summary>The server version is deprecated.</summary>
    Deprecated = 1,
    /// <summary>The server version is deleted and hidden by default.</summary>
    Deleted = 2
}

/// <summary>A configured MCP server version along with registry managed metadata.</summary>
public class McpServerEntry : McpServerDetail
{
    /// <summary>Server lifecycle status. Defaults to <see cref="McpServerStatus.Active"/>.</summary>
    [JsonIgnore]
    public McpServerStatus Status { get; set; } = McpServerStatus.Active;
    /// <summary>Optional message explaining the status (e.g. deprecation reason).</summary>
    [JsonIgnore]
    public string? StatusMessage { get; set; }
    /// <summary>Timestamp when the status was last changed.</summary>
    [JsonIgnore]
    public DateTimeOffset? StatusChangedAt { get; set; }
    /// <summary>Timestamp when the server version was first published.</summary>
    [JsonIgnore]
    public DateTimeOffset? PublishedAt { get; set; }
    /// <summary>Timestamp when the server version was last updated.</summary>
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt { get; set; }
}

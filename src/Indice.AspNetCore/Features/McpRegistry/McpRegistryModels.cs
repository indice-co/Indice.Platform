using System.Text.Json.Serialization;

namespace Indice.AspNetCore.Features.McpRegistry;

/// <summary>Static representation of an MCP server version (the <c>server.json</c> format).</summary>
public class McpServerDetail
{
    /// <summary>JSON Schema URI for this server.json format.</summary>
    [JsonPropertyName("$schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Schema { get; set; }
    /// <summary>Server name in reverse-DNS format. Must contain exactly one forward slash separating namespace from server name (e.g. <c>io.github.user/weather</c>).</summary>
    public string Name { get; set; } = null!;
    /// <summary>Clear human-readable explanation of server functionality.</summary>
    public string Description { get; set; } = null!;
    /// <summary>Optional human-readable title or display name for the MCP server.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
    /// <summary>Version string for this server. Should follow semantic versioning.</summary>
    public string Version { get; set; } = null!;
    /// <summary>Optional URL to the server's homepage, documentation, or project website.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebsiteUrl { get; set; }
    /// <summary>Optional repository metadata for the MCP server source code.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpRepository? Repository { get; set; }
    /// <summary>Optional set of sized icons that the client can display in a user interface.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpIcon>? Icons { get; set; }
    /// <summary>Packages that can be installed and run locally.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpPackage>? Packages { get; set; }
    /// <summary>Remote endpoints of the server.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpRemote>? Remotes { get; set; }
}

/// <summary>Repository metadata for the MCP server source code.</summary>
public class McpRepository
{
    /// <summary>Repository URL for browsing source code.</summary>
    public string Url { get; set; } = null!;
    /// <summary>Repository hosting service identifier (e.g. <c>github</c>).</summary>
    public string Source { get; set; } = null!;
    /// <summary>Repository identifier from the hosting service.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
    /// <summary>Optional relative path from repository root to the server location.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subfolder { get; set; }
}

/// <summary>An optionally-sized icon that can be displayed in a user interface.</summary>
public class McpIcon
{
    /// <summary>An HTTPS URI pointing to an icon resource.</summary>
    public string Src { get; set; } = null!;
    /// <summary>Optional MIME type override.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }
    /// <summary>Optional sizes in WxH format or <c>any</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Sizes { get; set; }
    /// <summary>Optional theme specifier (<c>light</c> or <c>dark</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Theme { get; set; }
}

/// <summary>A package that can be downloaded and run locally.</summary>
public class McpPackage
{
    /// <summary>Registry type indicating how to download packages (e.g. <c>npm</c>, <c>pypi</c>, <c>oci</c>, <c>nuget</c>, <c>mcpb</c>).</summary>
    public string RegistryType { get; set; } = null!;
    /// <summary>Base URL of the package registry.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistryBaseUrl { get; set; }
    /// <summary>Package identifier - either a package name or a URL.</summary>
    public string Identifier { get; set; } = null!;
    /// <summary>Package version. Must be a specific version.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }
    /// <summary>SHA-256 hash of the package file for integrity verification.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileSha256 { get; set; }
    /// <summary>A hint to help clients determine the appropriate runtime (e.g. <c>npx</c>, <c>uvx</c>, <c>docker</c>, <c>dnx</c>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RuntimeHint { get; set; }
    /// <summary>Transport protocol configuration for the package.</summary>
    public McpTransport Transport { get; set; } = new();
    /// <summary>Arguments passed to the package's runtime command.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpArgument>? RuntimeArguments { get; set; }
    /// <summary>Arguments passed to the package's binary.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpArgument>? PackageArguments { get; set; }
    /// <summary>Environment variables to be set when running the package.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpKeyValueInput>? EnvironmentVariables { get; set; }
}

/// <summary>Transport protocol configuration.</summary>
public class McpTransport
{
    /// <summary>Transport type: <c>stdio</c>, <c>streamable-http</c> or <c>sse</c>.</summary>
    public string Type { get; set; } = "stdio";
    /// <summary>URL template. Required for <c>streamable-http</c> and <c>sse</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
    /// <summary>HTTP headers to include.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpKeyValueInput>? Headers { get; set; }
}

/// <summary>Transport protocol configuration for a remote endpoint.</summary>
public class McpRemote : McpTransport
{
    /// <summary>Creates a new <see cref="McpRemote"/> defaulting to <c>streamable-http</c>.</summary>
    public McpRemote() => Type = "streamable-http";
    /// <summary>Configuration variables that can be referenced in URL template <c>{curly_braces}</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, McpInput>? Variables { get; set; }
}

/// <summary>A user or configuration provided input.</summary>
public class McpInput
{
    /// <summary>A description of the input.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    /// <summary>Whether the input is required.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRequired { get; set; }
    /// <summary>The input format: <c>string</c>, <c>number</c>, <c>boolean</c> or <c>filepath</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; set; }
    /// <summary>The value for the input. If not set, the user may be prompted to provide a value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }
    /// <summary>Indicates whether the input is a secret value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSecret { get; set; }
    /// <summary>The default value for the input.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Default { get; set; }
    /// <summary>A placeholder for the input.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Placeholder { get; set; }
    /// <summary>A list of possible values for the input.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Choices { get; set; }
}

/// <summary>An input which supports variable substitution in its value.</summary>
public class McpInputWithVariables : McpInput
{
    /// <summary>A map of variable names to their values.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, McpInput>? Variables { get; set; }
}

/// <summary>A named input (header or environment variable).</summary>
public class McpKeyValueInput : McpInputWithVariables
{
    /// <summary>Name of the header or environment variable.</summary>
    public string Name { get; set; } = null!;
}

/// <summary>A command line argument (<c>positional</c> or <c>named</c>).</summary>
public class McpArgument : McpInputWithVariables
{
    /// <summary>The argument type: <c>positional</c> or <c>named</c>.</summary>
    public string Type { get; set; } = "positional";
    /// <summary>The flag name, including any leading dashes. Required for <c>named</c> arguments.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    /// <summary>An identifier for a positional argument.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ValueHint { get; set; }
    /// <summary>Whether the argument can be repeated.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRepeated { get; set; }
}

/// <summary>API response with separated server data and registry metadata.</summary>
public class McpServerResponse
{
    /// <summary>The server details.</summary>
    public McpServerDetail Server { get; set; } = null!;
    /// <summary>Registry managed metadata.</summary>
    [JsonPropertyName("_meta")]
    public McpServerResponseMeta Meta { get; set; } = new();
}

/// <summary>Registry managed metadata.</summary>
public class McpServerResponseMeta
{
    /// <summary>Official MCP registry metadata.</summary>
    [JsonPropertyName("io.modelcontextprotocol.registry/official")]
    public McpOfficialMeta Official { get; set; } = new();
}

/// <summary>Official MCP registry metadata.</summary>
public class McpOfficialMeta
{
    /// <summary>Server lifecycle status (<c>active</c>, <c>deprecated</c>, <c>deleted</c>).</summary>
    public string Status { get; set; } = "active";
    /// <summary>Optional message explaining the status.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusMessage { get; set; }
    /// <summary>Timestamp when the server status was last updated.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? StatusChangedAt { get; set; }
    /// <summary>Timestamp when the server was first published to the registry.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? PublishedAt { get; set; }
    /// <summary>Timestamp when the server entry was last updated.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>Whether this is the latest version of the server.</summary>
    public bool IsLatest { get; set; }
}

/// <summary>A list of MCP servers.</summary>
public class McpServerList
{
    /// <summary>The servers.</summary>
    public List<McpServerResponse> Servers { get; set; } = [];
    /// <summary>Paging metadata.</summary>
    public McpServerListMetadata Metadata { get; set; } = new();
}

/// <summary>Paging metadata.</summary>
public class McpServerListMetadata
{
    /// <summary>Pagination cursor for retrieving the next page of results. Null when there are no more results.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; set; }
    /// <summary>Number of items in current page.</summary>
    public int Count { get; set; }
}

/// <summary>Error response of the MCP registry.</summary>
/// <param name="Error">The error message.</param>
public record McpRegistryError(string Error);

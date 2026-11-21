using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows;
using Indice.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Indice.Features.Agents.Core.Services.Risk;

/// <summary>One executed catalog query, as returned by the risk MCP server's <c>run_query</c> tool.</summary>
public sealed class RiskQueryResult
{
    /// <summary>The catalog id, e.g. <c>Q-RISK-07</c>.</summary>
    [JsonPropertyName("queryId")] public string QueryId { get; set; } = string.Empty;

    /// <summary>The logical database the query ran against.</summary>
    [JsonPropertyName("database")] public string Database { get; set; } = string.Empty;

    /// <summary>When the query ran, UTC.</summary>
    [JsonPropertyName("executedAt")] public DateTimeOffset ExecutedAt { get; set; }

    /// <summary>The parameters as bound by the server.</summary>
    [JsonPropertyName("parameters")] public Dictionary<string, JsonElement> Parameters { get; set; } = [];

    /// <summary>SHA-256 of the executed SQL text; matches the server's <c>describe_query</c>.</summary>
    [JsonPropertyName("sqlSha256")] public string SqlSha256 { get; set; } = string.Empty;

    /// <summary>The number of rows returned.</summary>
    [JsonPropertyName("rowCount")] public int RowCount { get; set; }

    /// <summary>True when more rows existed than the server returns: the result is partial and the report must say so.</summary>
    [JsonPropertyName("truncated")] public bool Truncated { get; set; }

    /// <summary>The column names, in result order.</summary>
    [JsonPropertyName("columns")] public List<string> Columns { get; set; } = [];

    /// <summary>The rows. Dates are UTC ISO 8601 strings.</summary>
    [JsonPropertyName("rows")] public List<Dictionary<string, JsonElement>> Rows { get; set; } = [];

    /// <summary>How long the database call took, in milliseconds.</summary>
    [JsonPropertyName("durationMs")] public long DurationMs { get; set; }

    /// <summary>The error the server reported instead of rows, or <c>null</c> when the query succeeded.</summary>
    [JsonPropertyName("error")] public string? Error { get; set; }

    /// <summary>Whether the query succeeded.</summary>
    [JsonIgnore] public bool Succeeded => Error is null;
}

/// <summary>A query the investigation could not run, as a failure the workflow can report.</summary>
public sealed class RiskQueryException : Exception
{
    /// <summary>Creates the exception.</summary>
    public RiskQueryException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Runs the vetted catalog queries of the risk MCP server.</summary>
public interface IRiskQueryClient
{
    /// <summary>Opens a session for one investigation. Queries run through the session share one MCP connection.</summary>
    /// <param name="correlationId">Ties the queries to the investigation in the server's audit log, e.g. the conversation id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IRiskQuerySession> OpenSessionAsync(string correlationId, CancellationToken cancellationToken = default);
}

/// <summary>One investigation's connection to the risk MCP server.</summary>
public interface IRiskQuerySession : IAsyncDisposable
{
    /// <summary>
    /// Runs a catalog query. A query the server rejects or that fails comes back with <see cref="RiskQueryResult.Error"/> set,
    /// so the caller can mark one section of the report incomplete and carry on. A transport failure throws.
    /// </summary>
    /// <param name="queryId">The catalog id, e.g. <c>Q-RISK-07</c>.</param>
    /// <param name="parameters">The query parameters. Dates must be <see cref="DateTimeOffset"/>; the client sends them as ISO 8601 with an offset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<RiskQueryResult> RunAsync(string queryId, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}

/// <summary>The MCP-backed <see cref="IRiskQueryClient"/>.</summary>
public sealed class RiskQueryClient : IRiskQueryClient
{
    /// <summary>The key under which the risk MCP client is registered.</summary>
    public const string McpServiceKey = "risk";

    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentsClaimsPrincipalSelector _claimsPrincipalSelector;
    private readonly ILogger<RiskQueryClient> _logger;

    /// <summary>Creates a new <see cref="RiskQueryClient"/>.</summary>
    public RiskQueryClient([FromKeyedServices(McpServiceKey)] IMcpClientFactory mcpClientFactory, AgentsClaimsPrincipalSelector claimsPrincipalSelector, ILogger<RiskQueryClient> logger) {
        _mcpClientFactory = mcpClientFactory;
        _claimsPrincipalSelector = claimsPrincipalSelector;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IRiskQuerySession> OpenSessionAsync(string correlationId, CancellationToken cancellationToken = default) {
        var user = _claimsPrincipalSelector();
        // The MCP server authenticates this host as a machine client; the person is passed along only for its audit log.
        var requestedBy = user?.FindSubjectId() ?? user?.FindFirst("name")?.Value ?? "unknown";
        var client = await _mcpClientFactory.CreateAsync(cancellationToken);
        return new Session(client, requestedBy, correlationId, _logger);
    }

    private sealed class Session(McpClient client, string requestedBy, string correlationId, ILogger logger) : IRiskQuerySession
    {
        public async Task<RiskQueryResult> RunAsync(string queryId, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default) {
            var arguments = new Dictionary<string, object?> {
                ["queryId"] = queryId,
                ["parameters"] = parameters.ToDictionary(pair => pair.Key, pair => pair.Value is DateTimeOffset date ? date.ToUniversalTime().ToString("O") : pair.Value),
                ["requestedBy"] = requestedBy,
                ["correlationId"] = correlationId
            };
            CallToolResult response;
            try {
                response = await client.CallToolAsync("run_query", arguments, cancellationToken: cancellationToken);
            } catch (Exception exception) when (exception is not OperationCanceledException) {
                throw new RiskQueryException($"The risk MCP server could not be reached while running {queryId}.", exception);
            }
            if (response.IsError == true) {
                var error = string.Concat(response.Content.OfType<TextContentBlock>().Select(block => block.Text)).Trim();
                logger.LogWarning("Risk query {QueryId} was refused or failed: {Error}", queryId, error);
                return new RiskQueryResult { QueryId = queryId, Error = string.IsNullOrWhiteSpace(error) ? "The query failed." : error };
            }
            var payload = response.StructuredContent
                          ?? response.Content.OfType<TextContentBlock>().Select(block => TryParse(block.Text)).FirstOrDefault(element => element is not null);
            if (payload is not { ValueKind: JsonValueKind.Object } element) {
                throw new RiskQueryException($"The risk MCP server returned an unreadable result for {queryId}.");
            }
            return element.Deserialize<RiskQueryResult>(JsonSerializerOptions.Web)
                   ?? throw new RiskQueryException($"The risk MCP server returned an empty result for {queryId}.");
        }

        private static JsonElement? TryParse(string? text) {
            try {
                return string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text).RootElement.Clone();
            } catch (JsonException) {
                return null;
            }
        }

        public ValueTask DisposeAsync() => client.DisposeAsync();
    }
}

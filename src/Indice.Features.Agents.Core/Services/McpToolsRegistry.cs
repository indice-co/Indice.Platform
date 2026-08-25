using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Connects to configured external MCP servers on the first <see cref="GetToolsAsync"/> call,
/// fetches their tool manifests, and returns the cached union on every subsequent call.
/// </summary>
/// <remarks>
/// Register as a singleton. If an endpoint is unreachable the registry logs a warning and
/// skips that server so the rest of the pipeline degrades gracefully.
/// </remarks>
public sealed class McpToolsRegistry : IMcpToolsRegistry, IAsyncDisposable
{
    private readonly IDictionary<string, AgentsOptions.McpServiceOptions> _servers;
    private readonly ConcurrentDictionary<string, IReadOnlyList<AITool>> _serverTools = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<McpToolsRegistry> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly IDistributedCache _cache;

    /// <summary>Creates a new <see cref="McpToolsRegistry"/>.</summary>
    /// <param name="options">The options for configuring the MCP services.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public McpToolsRegistry(IOptions<AgentsOptions> options, IHttpContextAccessor httpContextAccessor, IDistributedCache cache, ILogger<McpToolsRegistry> logger) {
        _httpContextAccessor = httpContextAccessor;
        _servers = options.Value.Mcp.Services;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AITool>> GetToolsAsync(string service, CancellationToken cancellationToken = default) {

        if (_servers.Count == 0) return [];
        _servers.TryGetValue(service, out var mcpServiceOptions);

        if (mcpServiceOptions is null) return [];

        await _initLock.WaitAsync(cancellationToken);
        try {

            if (_serverTools.TryGetValue(service, out var tools))
                return tools;
            try {
                HttpClientTransport transport;
                if (mcpServiceOptions.OAuth is { } oauth) {
                    var handler = new ClientCredentialsBearerHandler(oauth.TokenEndpoint, oauth.ClientId, oauth.ClientSecret, oauth.Scope, _httpContextAccessor, _cache);
                    var http = new HttpClient(handler);
                    transport = new HttpClientTransport(
                        new HttpClientTransportOptions {
                            Endpoint = new Uri(mcpServiceOptions.Endpoint),
                            TransportMode = HttpTransportMode.StreamableHttp,
                        },
                        http,
                        ownsHttpClient: true); // transport disposes the client
                } else {
                    var opts = new HttpClientTransportOptions {
                        Endpoint = new Uri(mcpServiceOptions.Endpoint),
                        TransportMode = HttpTransportMode.StreamableHttp,
                    };
                    var header = _httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();
                    if (!string.IsNullOrWhiteSpace(header)) {
                        opts.AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = header };
                    }
                    transport = new HttpClientTransport(opts);
                }

                await using var transportScope = transport;
                await using var client = await McpClient.CreateAsync(transportScope, cancellationToken: cancellationToken);
                var clientTools = await client.ListToolsAsync(cancellationToken: cancellationToken);

                _serverTools.TryAdd(service, clientTools.AsReadOnly());
                _logger.LogInformation("External MCP {Url}: discovered {Count} tool(s).", mcpServiceOptions.Endpoint, clientTools.Count);
                return clientTools.AsReadOnly();
            } catch (UriFormatException ex) {
                _logger.LogWarning(ex, "External MCP {Url}: failed to connect or list tools; skipping.", mcpServiceOptions.Endpoint);
            } catch (HttpRequestException ex) {
                _logger.LogWarning(ex, "External MCP {Url}: failed to connect or list tools; skipping.", mcpServiceOptions.Endpoint);
            } catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                _logger.LogWarning(ex, "External MCP {Url}: failed to connect or list tools; skipping.", mcpServiceOptions.Endpoint);
            }
            return [];
        } finally {
            _initLock.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
        _initLock.Dispose();
    }
}
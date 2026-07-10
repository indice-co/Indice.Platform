using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Workflows.Mcp;

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
    private readonly IReadOnlyList<AgentsOptions.ExternalMcpServer> _servers;
    private readonly ILogger<McpToolsRegistry> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IReadOnlyList<AITool>? _cached;
    private List<McpClient>? _clients;
    private readonly IDistributedCache _cache;
    /// <summary>Creates a new <see cref="McpToolsRegistry"/>.</summary>
    public McpToolsRegistry(IOptions<AgentsOptions> options, IHttpContextAccessor httpContextAccessor, ILogger<McpToolsRegistry> logger, IDistributedCache cache) {
        _httpContextAccessor = httpContextAccessor;
        _servers = options.Value.ExternalMcp.Servers;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default) {
        //if (_cached is not null) return _cached;
        if (_servers.Count == 0) return _cached = [];
        await _initLock.WaitAsync(cancellationToken);
        try {
            //if (_cached is not null) return _cached;
            var allTools = new List<AITool>();
            var clients = new List<McpClient>();
            foreach (var server in _servers) {
                try {
                    HttpClientTransport transport;
                    //if (server.OAuth is { } oauth) {
                    //    var handler = new ClientCredentialsBearerHandler(oauth.TokenEndpoint, oauth.ClientId, oauth.ClientSecret, oauth.Scope, _httpContextAccessor, _cache);
                    //    var http = new HttpClient(handler);
                    //    transport = new HttpClientTransport(
                    //        new HttpClientTransportOptions {
                    //            Endpoint = new Uri(server.Url),
                    //            TransportMode = HttpTransportMode.StreamableHttp,
                    //        },
                    //        http,
                    //        ownsHttpClient: true); // transport disposes the client
                    //} else {

                    //}
                    var opts = new HttpClientTransportOptions {
                        Endpoint = new Uri(server.Url),
                        TransportMode = HttpTransportMode.StreamableHttp,
                    };
                    var header = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
                    opts.AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = header };

                    transport = new HttpClientTransport(opts);
                    var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
                    clients.Add(client);

                    var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                    allTools.AddRange(tools);
                    _logger.LogInformation("External MCP {Url}: discovered {Count} tool(s).", server.Url, tools.Count);
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "External MCP {Url}: failed to connect or list tools; skipping.", server.Url);
                }
            }

            _clients = clients;
            return _cached = allTools;
        } finally {
            _initLock.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
        _initLock.Dispose();

        if (_clients is null) return;

        foreach (var client in _clients) {
            await client.DisposeAsync();
        }
    }
}

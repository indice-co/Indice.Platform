using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Mcp;

/// <summary>
/// Provides <see cref="AITool"/> instances discovered from one or more external MCP servers.
/// </summary>
public interface IMcpToolsRegistry
{
    /// <summary>
    /// Returns the cached list of tools from all configured MCP servers.
    /// Connects and fetches on the first call; returns an empty list when no
    /// endpoints are configured or when all connections fail.
    /// </summary>
    Task<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default);
}

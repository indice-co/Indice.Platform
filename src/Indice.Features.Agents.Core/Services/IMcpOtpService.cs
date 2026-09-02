using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Provides abstraction for OTP operations via MCP or external service.
/// Supports sending OTP via phone or email and validating received OTP.
/// </summary>
public interface IMcpToolsRegistry
{
    /// <summary>
    /// Returns the cached list of tools from all configured MCP servers.
    /// Connects and fetches on the first call; returns an empty list when no
    /// endpoints are configured or when all connections fail.
    /// </summary>
    Task<IReadOnlyList<AITool>> GetToolsAsync(string service, CancellationToken cancellationToken = default);
}
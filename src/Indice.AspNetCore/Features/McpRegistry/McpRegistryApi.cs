using Indice.AspNetCore.Features.McpRegistry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Endpoint mappings for an MCP registry that follows the v0.1 MCP registry specification
/// and is consumable by GitHub Copilot as a custom (enterprise/organization) MCP registry.
/// </summary>
public static class McpRegistryApi
{
    /// <summary>
    /// Maps the read-only MCP registry endpoints:
    /// <list type="bullet">
    /// <item><c>GET v0.1/servers</c></item>
    /// <item><c>GET v0.1/servers/{serverName}/versions</c></item>
    /// <item><c>GET v0.1/servers/{serverName}/versions/{version}</c> (including <c>latest</c>)</item>
    /// </list>
    /// The data are served from the <see cref="McpRegistryOptions"/> which are bound to <c>IConfiguration</c>.
    /// </summary>
    /// <remarks>
    /// When <see cref="McpRegistryOptions.EnableCors"/> is set, the endpoints use the <see cref="McpRegistryOptions.CorsPolicyName"/> CORS policy,
    /// so the CORS middleware must be added to the pipeline (<c>app.UseCors()</c>).
    /// </remarks>
    /// <param name="routes">The endpoint route builder.</param>
    /// <param name="pathPrefix">Optional path prefix. Overrides the <see cref="McpRegistryOptions.PathPrefix"/>.</param>
    public static RouteGroupBuilder MapMcpRegistry(this IEndpointRouteBuilder routes, string? pathPrefix = null) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<McpRegistryOptions>>().Value;
        var group = routes.MapGroup(pathPrefix ?? options.PathPrefix ?? "/");
        group.WithTags("McpRegistry");
        if (!string.IsNullOrWhiteSpace(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        if (options.ExcludeFromDescription) {
            group.ExcludeFromDescription();
        }
        if (options.EnableCors) {
            group.RequireCors(McpRegistryOptions.CorsPolicyName);
        }

        var servers = group.MapGroup("v0.1/servers");
        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy)) {
            servers.AllowAnonymous();
        } else {
            servers.RequireAuthorization(options.AuthorizationPolicy);
        }

        servers.MapGet(string.Empty, McpRegistryHandlers.ListServers)
               .WithName("McpRegistryListServers")
               .WithSummary("List MCP servers.")
               .WithDescription("Returns a paged list of the registered MCP servers.");

        servers.MapGet("{serverName}/versions", McpRegistryHandlers.ListServerVersions)
               .WithName("McpRegistryListServerVersions")
               .WithSummary("List all versions of an MCP server.")
               .WithDescription("Returns all available versions for a specific MCP server, newest first. The server name must be URL-encoded (e.g. com.example%2Fmy-server).");

        servers.MapGet("{serverName}/versions/{version}", McpRegistryHandlers.GetServerVersion)
               .WithName("McpRegistryGetServerVersion")
               .WithSummary("Get specific MCP server version.")
               .WithDescription("Returns detailed information about a specific version of an MCP server. Use the special version 'latest' to get the latest version. The server name must be URL-encoded (e.g. com.example%2Fmy-server).");

        // Tolerate clients that do not URL-encode the forward slash of the server name.
        servers.MapGet("{serverNamespace}/{serverName}/versions", McpRegistryHandlers.ListServerVersionsUnencoded).ExcludeFromDescription();
        servers.MapGet("{serverNamespace}/{serverName}/versions/{version}", McpRegistryHandlers.GetServerVersionUnencoded).ExcludeFromDescription();
        return group;
    }
}

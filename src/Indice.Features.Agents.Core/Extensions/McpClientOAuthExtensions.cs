using Duende.AccessTokenManagement;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>Provides OAuth-related extension methods for configuring an <see cref="McpClient"/> in the dependency injection container.</summary>
public static class McpClientOAuthExtensions
{
    /// <summary>
    /// Configures the <see cref="McpClient"/> to use an HTTP transport with the specified endpoint, authenticating with the
    /// OAuth client_credentials flow (machine-to-machine, no user present). Access tokens are acquired and attached automatically
    /// via <c>Duende.AccessTokenManagement</c>.
    /// </summary>
    /// <param name="builder">The MCP client builder.</param>
    /// <param name="endpoint">The MCP server endpoint.</param>
    /// <param name="configureCredentials">Configures the token client (token endpoint, client id/secret, scope).</param>
    /// <param name="configure">Optional additional configuration for the underlying <see cref="HttpClientTransportOptions"/>.</param>
    public static IMcpClientBuilder WithClientCredentialsHttpTransport(
        this IMcpClientBuilder builder,
        Uri endpoint,
        Action<ClientCredentialsClient> configureCredentials,
        Action<IServiceProvider, HttpClientTransportOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(configureCredentials);

        var clientName = $"mcp-{builder.Name}";
        var clientCredentialsClientName = ClientCredentialsClientName.Parse($"mcp-{builder.Name}-auth");
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddClientCredentialsTokenManagement()
                        .AddClient(clientName, configureCredentials);
        return WithClientCredentialsHttpTransport(builder, endpoint, clientCredentialsClientName, configure);
    }

    /// <summary>
    /// Configures the <see cref="McpClient"/> to use an HTTP transport with the specified endpoint, authenticating with the
    /// OAuth client_credentials flow (machine-to-machine, no user present). Access tokens are acquired and attached automatically
    /// via <c>Duende.AccessTokenManagement</c>.
    /// </summary>
    /// <remarks>Reuse an existing client credentials client if available.</remarks>
    /// <param name="builder">The MCP client builder.</param>
    /// <param name="endpoint">The MCP server endpoint.</param>
    /// <param name="clientCredentialsClientName">The name of the client credentials client.</param>
    /// <param name="configure">Optional additional configuration for the underlying <see cref="HttpClientTransportOptions"/>.</param>
    /// <returns>The MCP client builder.</returns>
    public static IMcpClientBuilder WithClientCredentialsHttpTransport(
        this IMcpClientBuilder builder,
        Uri endpoint,
        ClientCredentialsClientName clientCredentialsClientName,
        Action<IServiceProvider, HttpClientTransportOptions>? configure = null) {
        var clientName = $"mcp-{builder.Name}";
        builder.Services.AddHttpClient(clientName)
                        .AddClientCredentialsTokenHandler(clientCredentialsClientName);

        return builder.WithHttpTransport(
            endpoint,
            sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(clientName),
            configure);
    }
}

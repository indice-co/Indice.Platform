using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>Options for registering an <see cref="McpClient"/> in the dependency injection container.</summary>
public class McpClientRegistrationOptions
{
    /// <summary>Gets or sets a value indicating whether the <see cref="McpClient"/> should share its session across multiple instances.</summary>
    public bool ShareSession { get; set; }
    
    /// <summary>Gets or sets a factory function for creating the <see cref="IClientTransport"/> used by the <see cref="McpClient"/>.</summary>
    public Func<IServiceProvider, IClientTransport> TransportFactory { get; set; } = _ => throw new NotImplementedException();

    /// <summary>Gets or sets the options for configuring the <see cref="McpClient"/>.</summary>
    public McpClientOptions Client { get; set; } = new();
}

using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace Indice.Features.Agents.Core.Extensions;

/// <summary>Defines a builder for configuring and registering an <see cref="McpClient"/> in the dependency injection container.</summary>
public interface IMcpClientBuilder
{
    /// <summary>Gets the <see cref="IServiceCollection"/> to which the <see cref="McpClient"/> is being added.</summary>
    IServiceCollection Services { get; }
    /// <summary>Gets the name of the <see cref="McpClient"/> being configured.</summary>
    string Name { get; }
}

internal sealed class McpClientBuilder(IServiceCollection services, string name) : IMcpClientBuilder
{
    public IServiceCollection Services { get; } = services;
    public string Name { get; } = name;
}

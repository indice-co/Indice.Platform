using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Agents.Core.Providers;

/// <summary>
/// Registration-time builder used to plug model providers into the agents feature, in the same spirit as the
/// <c>DbContextOptionsBuilder</c> of EF Core: provider packages add a <c>Use*</c> extension method on it.
/// </summary>
/// <example>
/// <code>
/// services.AddAgents(configuration, options => options.ConfigureModelProviders = providers => providers.UseFreeLLM());
/// </code>
/// </example>
public sealed class AgentsModelProviderBuilder
{
    /// <summary>Creates a new <see cref="AgentsModelProviderBuilder"/>.</summary>
    /// <param name="services">The service collection the providers are registered in.</param>
    public AgentsModelProviderBuilder(IServiceCollection services) {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>The service collection the providers are registered in.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Registers <typeparamref name="TProvider"/> as a model provider. Registering the same provider type twice has no effect.</summary>
    /// <typeparam name="TProvider">The provider type. See <see cref="IAgentsModelProvider"/> for the rules it must follow.</typeparam>
    /// <returns>The builder, for chaining.</returns>
    public AgentsModelProviderBuilder AddModelProvider<TProvider>() where TProvider : class, IAgentsModelProvider {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAgentsModelProvider, TProvider>());
        return this;
    }
}

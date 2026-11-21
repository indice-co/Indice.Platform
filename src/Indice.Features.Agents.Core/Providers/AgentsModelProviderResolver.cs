using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Providers;

/// <summary>Decides which <see cref="IAgentsModelProvider"/> serves a <see cref="AgentsModelRole"/>. Shared by the validator and the client factories.</summary>
internal static class AgentsModelProviderResolver
{
    /// <summary>The registered providers that claim the <paramref name="role"/>.</summary>
    public static List<IAgentsModelProvider> Claimants(IEnumerable<IAgentsModelProvider> providers, AgentsModelRole role) =>
        providers.Where(provider => provider.Serves(role)).ToList();

    /// <summary>
    /// Returns the provider that serves the <paramref name="role"/>: the one registered provider that claims it, or the built-in
    /// Azure AI Foundry provider when none does.
    /// </summary>
    /// <exception cref="InvalidOperationException">More than one registered provider claims the role.</exception>
    public static IAgentsModelProvider Resolve(IServiceProvider serviceProvider, AgentsModelRole role) {
        var claimants = Claimants(serviceProvider.GetServices<IAgentsModelProvider>(), role);
        return claimants.Count switch {
            0 => serviceProvider.GetRequiredService<AzureAIFoundryModelProvider>(),
            1 => claimants[0],
            _ => throw new InvalidOperationException($"The '{role}' model role is claimed by more than one model provider ({string.Join(", ", claimants.Select(provider => provider.Name))}). Configure a single provider per role.")
        };
    }
}

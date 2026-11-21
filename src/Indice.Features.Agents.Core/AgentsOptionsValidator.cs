using Indice.Features.Agents.Core.Providers;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core;

/// <summary>
/// Startup validator for <see cref="AgentsOptions"/>. Runs once on host start via <c>ValidateOnStart()</c>;
/// aggregates all failures into a single <see cref="ValidateOptionsResult"/> so the operator sees every
/// missing key on the first boot attempt, not one per restart.
/// </summary>
/// <remarks>
/// The Azure AI Foundry settings are required only for the <see cref="AgentsModelRole"/>s that no registered
/// <see cref="IAgentsModelProvider"/> serves.
/// </remarks>
public sealed class AgentsOptionsValidator : IValidateOptions<AgentsOptions>
{
    private readonly IReadOnlyList<IAgentsModelProvider> _providers;

    /// <summary>Creates a validator for a host with no additional model providers: Azure AI Foundry serves every role.</summary>
    public AgentsOptionsValidator() : this([]) {
    }

    /// <summary>Creates a validator that takes the registered model providers into account.</summary>
    /// <param name="providers">The registered model providers.</param>
    public AgentsOptionsValidator(IEnumerable<IAgentsModelProvider> providers) {
        _providers = (providers ?? throw new ArgumentNullException(nameof(providers))).ToList();
    }

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, AgentsOptions options) {
        var failures = new List<string>();

        var azureRoles = new List<AgentsModelRole>();
        foreach (var role in Enum.GetValues<AgentsModelRole>()) {
            var claimants = AgentsModelProviderResolver.Claimants(_providers, role);
            if (claimants.Count > 1) {
                failures.Add($"The '{role}' model role is claimed by more than one model provider ({string.Join(", ", claimants.Select(provider => provider.Name))}). Configure a single provider per role.");
            } else if (claimants.Count == 0) {
                azureRoles.Add(role);
            }
        }
        if (azureRoles.Count > 0) {
            // Only mention the fallback when there is another provider the operator may have meant to configure.
            var hint = _providers.Count > 0 ? $" (Azure AI Foundry serves the role(s) no registered model provider serves: {string.Join(", ", azureRoles)})" : string.Empty;
            if (string.IsNullOrWhiteSpace(options.AzureOpenAI.Endpoint))
                failures.Add($"Dex:AzureOpenAI:Endpoint must be configured (e.g. via dotnet user-secrets){hint}.");
            if (string.IsNullOrWhiteSpace(options.AzureOpenAI.ApiKey))
                failures.Add($"Dex:AzureOpenAI:ApiKey must be configured (e.g. via dotnet user-secrets){hint}.");
            foreach (var role in azureRoles) {
                var deployment = role switch {
                    AgentsModelRole.Reasoning => options.AzureOpenAI.Deployments.Reasoning,
                    AgentsModelRole.Fast => options.AzureOpenAI.Deployments.Fast,
                    _ => options.AzureOpenAI.Deployments.Embedding
                };
                if (string.IsNullOrWhiteSpace(deployment))
                    failures.Add($"Dex:AzureOpenAI:Deployments:{role} must be configured{hint}.");
            }
        }
        if (options.Taxonomy.Categories.Count == 0 || options.Taxonomy.Categories.Any(string.IsNullOrWhiteSpace))
            failures.Add("Dex:Taxonomy:Categories must contain at least one non-empty string.");
        if (options.Taxonomy.Languages.Count == 0 || options.Taxonomy.Languages.Any(string.IsNullOrWhiteSpace))
            failures.Add("Dex:Taxonomy:Languages must contain at least one non-empty string.");
        if (options.Taxonomy.RestrictedCategories.Any(string.IsNullOrWhiteSpace))
            failures.Add("Dex:Taxonomy:RestrictedCategories must not contain empty strings.");
        foreach (var restricted in options.Taxonomy.RestrictedCategories.Where(category => !string.IsNullOrWhiteSpace(category))) {
            if (!options.Taxonomy.Categories.Contains(restricted, StringComparer.OrdinalIgnoreCase))
                failures.Add($"Dex:Taxonomy:RestrictedCategories entry '{restricted}' must also be listed in Dex:Taxonomy:Categories, otherwise ingestion rejects it.");
        }
        if (options.Retrieval.RerankSnippetLength <= 0)
            failures.Add("Dex:Retrieval:RerankSnippetLength must be positive.");
        if (string.IsNullOrWhiteSpace(options.Routing.DefaultAgent))
            failures.Add("Dex:Routing:DefaultAgent must be configured (e.g. \"auto\").");
        if (options.SystemOne.Enabled && options.SystemOne.MinConfidence is <= 0 or > 1)
            failures.Add("Dex:SystemOne:MinConfidence must be greater than 0 and at most 1.");
        // The system-one decision model is Azure-only. It needs an endpoint and key of its own or the shared ones; when an Azure role
        // is already missing them the failures above cover it.
        if (options.SystemOne.Enabled && !string.IsNullOrWhiteSpace(options.AzureOpenAI.Deployments.SystemOne) && azureRoles.Count == 0) {
            if (string.IsNullOrWhiteSpace(options.AzureOpenAI.SystemOneEndpoint) && string.IsNullOrWhiteSpace(options.AzureOpenAI.Endpoint))
                failures.Add("Dex:AzureOpenAI:SystemOneEndpoint (or Dex:AzureOpenAI:Endpoint) must be configured when Dex:SystemOne:Enabled is true.");
            if (string.IsNullOrWhiteSpace(options.AzureOpenAI.SystemOneApiKey) && string.IsNullOrWhiteSpace(options.AzureOpenAI.ApiKey))
                failures.Add("Dex:AzureOpenAI:SystemOneApiKey (or Dex:AzureOpenAI:ApiKey) must be configured when Dex:SystemOne:Enabled is true.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

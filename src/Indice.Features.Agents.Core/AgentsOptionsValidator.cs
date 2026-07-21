using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core;

/// <summary>
/// Startup validator for <see cref="AgentsOptions"/>. Runs once on host start via <c>ValidateOnStart()</c>;
/// aggregates all failures into a single <see cref="ValidateOptionsResult"/> so the operator sees every
/// missing key on the first boot attempt, not one per restart.
/// </summary>
public sealed class AgentsOptionsValidator : IValidateOptions<AgentsOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, AgentsOptions options) {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AzureOpenAI.Endpoint))
            failures.Add("Dex:AzureOpenAI:Endpoint must be configured (e.g. via dotnet user-secrets).");
        if (string.IsNullOrWhiteSpace(options.AzureOpenAI.ApiKey))
            failures.Add("Dex:AzureOpenAI:ApiKey must be configured (e.g. via dotnet user-secrets).");
        if (string.IsNullOrWhiteSpace(options.AzureOpenAI.Deployments.Reasoning))
            failures.Add("Dex:AzureOpenAI:Deployments:Reasoning must be configured.");
        if (string.IsNullOrWhiteSpace(options.AzureOpenAI.Deployments.Fast))
            failures.Add("Dex:AzureOpenAI:Deployments:Fast must be configured.");
        if (string.IsNullOrWhiteSpace(options.AzureOpenAI.Deployments.Embedding))
            failures.Add("Dex:AzureOpenAI:Deployments:Embedding must be configured.");
        if (options.Taxonomy.Categories.Count == 0 || options.Taxonomy.Categories.Any(string.IsNullOrWhiteSpace))
            failures.Add("Dex:Taxonomy:Categories must contain at least one non-empty string.");
        if (options.Taxonomy.Languages.Count == 0 || options.Taxonomy.Languages.Any(string.IsNullOrWhiteSpace))
            failures.Add("Dex:Taxonomy:Languages must contain at least one non-empty string.");
        if (options.Retrieval.RerankSnippetLength <= 0)
            failures.Add("Dex:Retrieval:RerankSnippetLength must be positive.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}

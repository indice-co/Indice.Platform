namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Identifies which Azure OpenAI model tier an agent should use. The role selects both the deployment
/// (from <c>AgentsOptions.AzureOpenAI.Deployments</c>) and the base <c>ChatOptions</c> (from <see cref="ModelsOptions"/>)
/// that the <see cref="IAgentsFactory"/> clones.
/// </summary>
public enum AgentModelRole
{
    /// <summary>The reasoning-optimized deployment (intent classification, answer composition). Uses <c>Deployments.Reasoning</c> and <c>BaseReasoningModelOptions</c>.</summary>
    Reasoning,
    /// <summary>The fast, lower-cost deployment (query rewriting, reranking). Uses <c>Deployments.Fast</c> and <c>BaseFastModelOptions</c>.</summary>
    Fast
}

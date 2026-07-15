using Azure.AI.OpenAI;
using Microsoft.Agents.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Creates role-bound Microsoft Agent Framework agents from the shared <see cref="AzureOpenAIClient"/>,
/// centralizing the model-role → deployment/chat-options mapping, prompt rendering, and provider wiring that
/// each pipeline step would otherwise repeat inline.
/// </summary>
public interface IAgentsFactory
{
    /// <summary>Builds a reusable <see cref="AIAgent"/> from the supplied <paramref name="descriptor"/>.</summary>
    /// <param name="descriptor">Describes the agent's model role, prompt template, and providers to attach.</param>
    /// <returns>A configured agent. Per-conversation session binding is applied by the caller at run time.</returns>
    AIAgent Create(AgentDescriptor descriptor);
}

using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Describes the agent an <see cref="IAgentsFactory"/> should build: which model role to use, the prompt
/// template that supplies its instructions, and which cross-cutting providers to attach.
/// </summary>
public class AgentDescriptor
{
    /// <summary>The agent name surfaced to the Microsoft Agent Framework (e.g. <c>"DexIntentClassifier"</c>).</summary>
    public required string Name { get; init; }

    /// <summary>The model tier to use; selects the Azure OpenAI deployment and the base chat options to clone.</summary>
    public required AgentModelRole Role { get; init; }

    /// <summary>The prompt template name resolved by the renderer for the agent's instructions (e.g. <c>"IntentClassifier"</c>).</summary>
    public required string PromptTemplate { get; init; }

    /// <summary>Optional prompt-template variables, keyed by name (each key becomes a Handlebars variable).</summary>
    public IReadOnlyDictionary<string, object?>? PromptValues { get; init; }

    /// <summary>Whether to attach the session-backed chat-history provider. Defaults to <see langword="true"/>.</summary>
    public bool IncludeChatHistory { get; init; } = true;

    /// <summary>Whether to attach the user-claims AI context provider (injects the user-profile block). Defaults to <see langword="false"/>.</summary>
    public bool IncludeUserContext { get; init; }

    /// <summary>Optional hook to further tweak the cloned <see cref="ChatOptions"/> (e.g. tools, max output tokens) before the agent is built.</summary>
    public Action<ChatOptions>? ConfigureChatOptions { get; init; }
}

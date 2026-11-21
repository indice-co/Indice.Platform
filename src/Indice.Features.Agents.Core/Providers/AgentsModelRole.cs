namespace Indice.Features.Agents.Core.Providers;

/// <summary>The roles a model plays in the agents pipeline. Each role is served by exactly one <see cref="IAgentsModelProvider"/>.</summary>
/// <remarks>
/// The enum member names double as the keyed-service keys of the chat clients (<c>nameof(AgentsModelRole.Reasoning)</c>),
/// so they must not be renamed.
/// </remarks>
public enum AgentsModelRole
{
    /// <summary>Strong reasoning model used for intent classification, routing, tool calling and final answer composition.</summary>
    Reasoning = 0,

    /// <summary>Fast/cheap model used for query rewriting and reranking.</summary>
    Fast = 1,

    /// <summary>Embedding model used for chunk and query vectorization.</summary>
    Embedding = 2
}

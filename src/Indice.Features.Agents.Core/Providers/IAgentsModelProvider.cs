using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Providers;

/// <summary>
/// A source of language and embedding models for the agents pipeline. Azure AI Foundry is built into the core package and serves
/// every <see cref="AgentsModelRole"/> that no registered provider claims; additional providers (for example <c>UseFreeLLM()</c> from
/// the <c>Indice.Features.Agents.FreeLLM</c> package) are registered through <see cref="AgentsModelProviderBuilder"/>.
/// </summary>
/// <remarks>
/// <para>
/// A provider <em>claims</em> a role by returning <c>true</c> from <see cref="Serves"/>. A role can be claimed by at most one
/// registered provider; claiming the same role twice fails startup validation.
/// </para>
/// <para>
/// Implementations are registered as singletons and are resolved while <see cref="AgentsOptions"/> is being validated, so they must
/// <b>not</b> depend on <c>IOptions&lt;AgentsOptions&gt;</c> or <c>IOptions&lt;ModelsOptions&gt;</c> (neither in the constructor nor in
/// <see cref="Serves"/>): doing so causes a circular dependency. Read the provider's own options type instead, and resolve heavy or optional
/// dependencies (such as <c>IHttpClientFactory</c>) lazily from <see cref="IServiceProvider"/>.
/// </para>
/// </remarks>
public interface IAgentsModelProvider
{
    /// <summary>A short, human readable name of the provider, used in diagnostics (for example <c>FreeLLM</c>).</summary>
    string Name { get; }

    /// <summary>Whether this provider is configured to serve the <paramref name="role"/>.</summary>
    /// <param name="role">The model role.</param>
    bool Serves(AgentsModelRole role);

    /// <summary>Creates the chat client for a <see cref="AgentsModelRole.Reasoning"/> or <see cref="AgentsModelRole.Fast"/> role.</summary>
    /// <param name="role">The model role. Never <see cref="AgentsModelRole.Embedding"/>.</param>
    /// <returns>The chat client. Called once per role; the result is cached as a singleton.</returns>
    IChatClient CreateChatClient(AgentsModelRole role);

    /// <summary>Creates the embedding generator for the <see cref="AgentsModelRole.Embedding"/> role.</summary>
    /// <remarks>
    /// The vectors must match the width of the vector column of the knowledge store (<see cref="AgentsOptions.EmbeddingDimensionsDefault"/>).
    /// The generator should report its model id and dimensions through <see cref="EmbeddingGeneratorMetadata"/>.
    /// </remarks>
    /// <returns>The embedding generator. Called once; the result is cached as a singleton.</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator();
}

namespace Indice.Features.Agents.Core.Workflows.Abstractions;

/// <summary>Entry point for executing the Dex RAG pipeline against a single user question.</summary>
public interface IDexRunner
{
    /// <summary>Runs the configured pipeline against <paramref name="request"/> and projects the final envelope into <see cref="RagResult"/>.</summary>
    Task<RagResult> RunAsync(RagRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the configured pipeline against <paramref name="request"/>, yielding real-time
    /// <see cref="DexStreamEvent"/>s as it executes: a <see cref="DexStepEvent"/> as each step starts,
    /// <see cref="DexDeltaEvent"/>s as the answer streams, and a single terminal <see cref="DexFinalEvent"/>.
    /// </summary>
    IAsyncEnumerable<DexStreamEvent> RunStreamingAsync(RagRequest request, CancellationToken cancellationToken);
}

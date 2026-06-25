namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Batched embedding facade with transient-error retry.</summary>
public interface IEmbedder
{
    /// <summary>
    /// Returns one embedding per input text, in input order. Implementations batch internally per
    /// <c>IngestionOptions.EmbedBatchSize</c> and retry transient failures (429 / 5xx).
    /// </summary>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}

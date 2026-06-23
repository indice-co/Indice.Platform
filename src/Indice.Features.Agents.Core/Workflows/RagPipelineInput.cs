namespace Indice.Features.Agents.Core.Workflows;

/// <summary>
/// Marker payload for the first edge of a Dex RAG pipeline. The question itself lives on
/// <c>RagState.Question</c> (seeded by <c>DexRunner</c>), so the initial payload carries no data.
/// </summary>
public class RagPipelineInput
{
}

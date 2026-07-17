using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Events;

/// <summary>
/// Custom workflow event emitted by <c>AnswerComposer</c> for each text delta produced while the
/// reasoning model streams its answer. Observed by <c>DexRunner.RunStreamingAsync</c> and surfaced to
/// the caller as an SSE <c>delta</c> event. The non-streaming run path ignores it.
/// </summary>
/// <remarks>Creates a new <see cref="AnswerDeltaEvent"/> carrying a single answer text delta.</remarks>
public class AnswerDeltaEvent(string executorId, string delta) : ExecutorEvent(executorId, delta)
{
    /// <summary>The incremental answer text produced by this streaming update.</summary>
    public string Delta => (string)Data!;
}
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Mcp;

/// <summary>
/// Workflow event emitted by LLM agent steps for each streamed text token.
/// Surfaced as an SSE <c>delta</c> frame by the streaming runner; ignored by the non-streaming runner.
/// </summary>
/// <param name="Text">The text delta emitted by the model.</param>
public sealed class AnswerDeltaEvent(string Text) : WorkflowEvent
{
    /// <summary>The text delta emitted by the model.</summary>
    public string Text { get; } = Text;
}

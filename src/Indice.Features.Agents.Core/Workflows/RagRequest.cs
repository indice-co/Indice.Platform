namespace Indice.Features.Agents.Core.Workflows;

/// <summary>The input to <see cref="Abstractions.IDexRunner.RunAsync"/>.</summary>
public class RagRequest
{
    /// <summary>The end-user question being asked.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// The chat session this question belongs to. The pipeline's <see cref="SessionStoreChatHistoryProvider"/>
    /// loads the windowed conversation history for it during the run.
    /// </summary>
    public Guid SessionId { get; init; }
}

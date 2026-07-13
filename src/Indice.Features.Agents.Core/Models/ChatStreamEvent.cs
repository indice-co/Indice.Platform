namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Wire model for a single Server-Sent Event emitted while streaming a chat turn. The <see cref="Type"/>
/// discriminator mirrors the SSE <c>event:</c> name and tells the client which fields are populated:
/// <list type="bullet">
///   <item><term>step</term><description><see cref="Step"/> — a pipeline-progress label.</description></item>
///   <item><term>delta</term><description><see cref="Text"/> — an incremental chunk of the answer.</description></item>
///   <item><term>complete</term><description><see cref="SessionId"/>, <see cref="MessageId"/>, <see cref="Answer"/>, <see cref="Citations"/>, <see cref="Failed"/>, <see cref="FailureReason"/>.</description></item>
/// </list>
/// </summary>
public class ChatStreamEvent
{
    /// <summary>Event discriminator: <c>step</c>, <c>delta</c>, or <c>complete</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Human-friendly progress label; populated on <c>step</c> events.</summary>
    public string? Step { get; init; }

    /// <summary>Incremental answer text; populated on <c>delta</c> events.</summary>
    public string? Text { get; init; }

    /// <summary>The full answer; populated on the terminal <c>complete</c> event.</summary>
    public string? Answer { get; init; }

    /// <summary>Citations supporting the answer; populated on <c>complete</c>.</summary>
    public IReadOnlyList<Citation>? Citations { get; init; }

    /// <summary>Citations supporting the answer; populated on <c>complete</c>.</summary>
    public IReadOnlyList<SourceDocumentLink>? Sources { get; init; }

    /// <summary>Identifier of the session this turn belongs to; populated on <c>complete</c>.</summary>
    public Guid? SessionId { get; init; }

    /// <summary>Identifier of the persisted assistant message; populated on <c>complete</c>.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>True when a pipeline step threw and the workflow halted; populated on <c>complete</c>.</summary>
    public bool? Failed { get; init; }

    /// <summary>Error message from the step that threw; populated on <c>complete</c> when <see cref="Failed"/> is true.</summary>
    public string? FailureReason { get; init; }

    /// <summary>True when the turn was blocked by a session usage limit — <see cref="Answer"/> carries the predefined limit message and nothing was persisted; populated on <c>complete</c>.</summary>
    public bool? LimitReached { get; init; }

    /// <summary>Questions used in this session so far, for a <c>used/total</c> display; populated on <c>complete</c>. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsUsed { get; init; }

    /// <summary>Total questions allowed per session, for a <c>used/total</c> display; populated on <c>complete</c>. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsTotal { get; init; }
}

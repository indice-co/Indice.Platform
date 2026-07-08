namespace Indice.Features.Agents.Core.Models;

/// <summary>Response returned by both <c>POST /api/my/chats</c> and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatResponse
{
    /// <summary>Identifier of the session this turn belongs to.</summary>
    public Guid SessionId { get; init; }

    /// <summary>Identifier of the assistant message persisted for this turn.</summary>
    public Guid MessageId { get; init; }

    /// <summary>The pipeline's answer — grounded when in-scope, or the polite out-of-scope refusal text when not.</summary>
    public string? Answer { get; init; }

    /// <summary>Citations supporting the answer; empty for out-of-scope responses and on error.</summary>
    public IReadOnlyList<Citation> Citations { get; init; } = Array.Empty<Citation>();

    /// <summary>True when a pipeline step threw and the workflow halted. Out-of-scope is NOT a failure — its refusal text flows through <see cref="Answer"/>.</summary>
    public bool Failed { get; init; }

    /// <summary>Error message from the step that threw; <c>null</c> when <see cref="Failed"/> is false.</summary>
    public string? FailureReason { get; init; }

    /// <summary>True when the turn was blocked by a session usage limit — <see cref="Answer"/> carries the predefined limit message, nothing was persisted, and <see cref="MessageId"/> is empty.</summary>
    public bool LimitReached { get; init; }

    /// <summary>Questions used in this session so far, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsUsed { get; init; }

    /// <summary>Total questions allowed per session, for a <c>used/total</c> display. <c>null</c> when the message limit is disabled.</summary>
    public int? QuestionsTotal { get; init; }
}

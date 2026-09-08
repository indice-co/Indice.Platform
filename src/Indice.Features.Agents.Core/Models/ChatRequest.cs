namespace Indice.Features.Agents.Core.Models;

/// <summary>Body accepted by both <c>POST /api/my/chats</c> (creates the session inline) and <c>POST /api/my/chats/{id}/messages</c>.</summary>
public class ChatRequest
{
    /// <summary>The end-user message text.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Optional display name of the end-user. If not provided, the system will use a default name.</summary>
    public string? AuthorName { get; set; }

    /// <summary>Optional name of the agent to use for this chat. If not provided, the system will use a default agent.</summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Optional identifier of a record in an external system (e.g. a case number) the conversation is about.
    /// Surfaced by the hosting UI as the <c>refid</c> query string parameter and only honoured when the
    /// conversation is created: it grounds the customer-data workflow so it can skip discovering the purpose of
    /// the visit.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>The kind of record <see cref="ReferenceId"/> points at (the <c>reftype</c> query string parameter), e.g. <c>ServicePickup</c>.</summary>
    public string? ReferenceType { get; set; }
}
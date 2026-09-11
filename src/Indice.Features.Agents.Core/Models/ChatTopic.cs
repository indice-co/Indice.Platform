namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents the topic of a conversation.</summary>
/// <remarks>
/// Optional identifier of a record in an external system (e.g. a case number) the conversation is about.
/// Surfaced by the hosting UI as the <c>refid</c> query string parameter and only honoured when the
/// conversation is created: it grounds the workflow so it can skip discovering the purpose of
/// the visit.
/// </remarks>
public class ChatTopic
{
    /// <summary>The identifier of a record in an external system.</summary>
    public string? ReferenceId { get; set; }
    /// <summary>The kind of record <see cref="ReferenceId"/> points at (the <c>reftype</c> query string parameter), e.g. <c>ServicePickup</c>.</summary>
    public string? ReferenceType { get; set; }

    /// <summary>Returns an empty <see cref="ChatTopic"/> instance.</summary>
    public static ChatTopic Empty => new();
}
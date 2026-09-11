namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents the topic of a conversation.</summary>
/// <remarks>
/// Optional identifier of a record in an external system (e.g. a case number) the conversation is about.
/// Surfaced by the hosting UI as the <c>refid</c> query string parameter and only honoured when the
/// conversation is created: it grounds the workflow so it can skip discovering the purpose of
/// the visit.
/// </remarks>
/// <param name="ReferenceId">The identifier of a record in an external system.</param>
/// <param name="ReferenceType">The kind of record <paramref name="ReferenceId"/> points at (the <c>reftype</c> query string parameter), e.g. <c>ServicePickup</c>.</param>
public record ChatTopic(string? ReferenceId, string? ReferenceType)
{
    /// <summary>Returns an empty <see cref="ChatTopic"/> instance.</summary>
    public static ChatTopic Empty => new(null, null);
}

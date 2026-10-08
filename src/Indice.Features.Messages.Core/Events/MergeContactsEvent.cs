namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used to merge contacts.</summary>
public class MergeContactsEvent
{
    /// <summary>The Database Id of the primary contact.</summary>
    public Guid PrimaryContactId { get; set; }
    /// <summary>The list of duplicate contact IDs to merge with the primary contact.</summary>
    public List<Guid> DuplicateContactsIds { get; set; } = [];
}
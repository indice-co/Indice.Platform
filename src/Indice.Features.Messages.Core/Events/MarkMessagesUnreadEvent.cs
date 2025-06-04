using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Events;
/// <summary>Event used to mark messages as unread.</summary>
public class MarkMessagesUnreadEvent
{
    ///User code for the user who will have their messages marked as unread.
    public string UserCode { get; set; } = null!;
    /// <summary>Search term.</summary>
    public string? SearchTerm { get; set; }
    /// <summary>Filter criteria.</summary>
    public MessagesFilter? Filter { get; set; }
}

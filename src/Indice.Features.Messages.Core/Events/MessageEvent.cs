using System.Diagnostics;
using Indice.Features.Messages.Core.Data.Models;

namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when a new campaign action occurs.</summary>
public class MessageEvent
{
    /// <summary>The unique identifier of the associated campaign.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>The unique identifier of the associated contact.</summary>
    public Guid ContactId { get; set; }
    /// <summary>The unique identifier of the message.</summary>
    public Guid? MessageId { get; set; }
    /// <summary>The type of the event.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The communication channel.</summary>
    public string Channel { get; set; } = string.Empty;
    /// <summary>The date and time when the event occurred.</summary>   
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets the database event model representation of this message event.</summary>
    public DbMessageEvent GetDbEvent() =>
        new DbMessageEvent() {
            Type = Type,
            Channel = Channel,
            MessageId = MessageId,
            CampaignId = CampaignId,
            ContactId = ContactId,
            CreatedOn = CreatedOn
        };

}
/// <summary>The event model used when a new campaign action occurs.</summary>
public enum MessageEventType
{
    /// <summary>The event type used when a new campaign message is created.</summary>
    Created,
    /// <summary>The event type used when a new contact is resolved.</summary>
    Sent,
    /// <summary>The event type used when a message is opened by the user.</summary>
    MarkedAsRead,
    /// <summary>The event type used when a message is deleted by the user.</summary>
    MarkedAsDeleted,
    /// <summary>The event type used when a message is marked as unread by the user.</summary>
    MarkedAsUnread,
}
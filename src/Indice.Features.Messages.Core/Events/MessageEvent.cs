

namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when a new campaign action occurs.</summary>
public class MessageEvent
{
    /// <summary>Gets or sets the unique identifier for the entity.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
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
    /// <summary>The Receiver of the message.</summary>
    public string Receiver { get; set; } = string.Empty;
    /// <summary>The date and time when the event occurred.</summary>   
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}
/// <summary>The event model used when a new campaign action occurs.</summary>
public enum MessageEventType
{
    /// <summary>The event type used when a new campaign message is created.</summary>
    Created,
    /// <summary>The event type used when a new contact is resolved.</summary>
    Sent,
    /// <summary>The event type used when a message is marked unread by the user.</summary>
    UnRead,
    /// <summary>The event type used when a message is marked as read by the user.</summary>
    Read,
    /// <summary>The event type used when a message is opened by the user.</summary>
    Opened,
    /// <summary>The event type used when a message is marked deleted by the user.</summary>
    Deleted,
    /// <summary>The event type used when a message is marked as delivered by an external provider.</summary>
    Delivered,
}
namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when a new campaign action occurs.</summary>
public class CampaignEvent
{
    /// <summary>The unique identifier of the associated campaign.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>The unique identifier of the associated contact.</summary>
    public Guid ContactId { get; set; }
    /// <summary>The type of the event.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The communication channel.</summary>
    public string Channel { get; set; } = string.Empty;
    /// <summary>The date and time when the event occurred.</summary>   
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}
/// <summary>The event model used when a new campaign action occurs.</summary>
public enum CampaignEventType { 
    /// <summary>The event type used when a new campaign message is created.</summary>
    Created,
    /// <summary>The event type used when a new contact is resolved.</summary>
    Sent
}
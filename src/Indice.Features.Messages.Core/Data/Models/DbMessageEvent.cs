namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Campaign Event entity.</summary>
public class DbMessageEvent
{
    /// <summary>The unique identifier of the campaign event.</summary>
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
    /// <summary>The recipient depending on the channel.</summary>
    /// <remarks>Phone number for sms, email for email or recipient Id for other channels.</remarks>
    public string Recipient { get; set; } = string.Empty;
    
    /// <summary>The date and time when the event occurred.</summary>   
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}

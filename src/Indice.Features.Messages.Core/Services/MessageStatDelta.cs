using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Services;

/// <summary>One successful send, waiting to be added to the send statistics.</summary>
public class MessageStatDelta
{
    /// <summary>The unique identifier of the campaign that the send belongs to.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>The communication channel.</summary>
    public MessageChannelKind Channel { get; set; }
    /// <summary>The UTC day of the send, taken from the time the message event occurred.</summary>
    public DateOnly Day { get; set; }
}

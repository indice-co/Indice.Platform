using Indice.Features.Messages.Core.Events;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>
/// Represents the response containing details about campaign messages, including the message content, contact
/// information, and associated channels.
/// </summary>
public class CampaignMessageResponse
{
    /// <summary>The unique identifier of the user message.</summary>
    public Guid Id { get; set; }
    /// <summary>Determines if a message is deleted by the user.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Determines if a message is read by the user.</summary>
    public bool IsRead { get; set; }
    /// <summary>Defines when the inbox message was read.</summary>
    public DateTimeOffset? ReadDate { get; set; }
    /// <summary>Defines when the inbox message was deleted.</summary>
    public DateTimeOffset? DeleteDate { get; set; }
    /// <summary>
    /// Contact details for the campaign.
    /// </summary>
    public Contact Contact { get; set; } = null!;
    /// <summary>
    /// Gets or sets the list of messages.
    /// </summary>
    public List<string> Channels { get; set; } = new List<string>();
}
/// <summary>
/// Represents the response containing details about campaign messages, including the message content, contact
/// information, and associated channels.
/// </summary>
public class CampaignMessageDetailsResponse : CampaignMessageResponse
{
    /// <summary>
    /// Gets or sets the list of messages .
    /// </summary>
    public List<MessageEvent> Events { get; set; } = new List<MessageEvent>();
}

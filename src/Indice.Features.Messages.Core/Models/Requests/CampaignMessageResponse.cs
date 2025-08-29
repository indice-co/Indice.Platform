using Indice.Features.Messages.Core.Events;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>
/// Represents the response containing details about campaign messages, including the message content, contact
/// information, and associated channels.
/// </summary>
public class CampaignMessageResponse
{
    /// <summary>The unique identifier of the user message.</summary>
    public Guid? MessageId { get; set; }
    /// <summary>The unique id of the contact.</summary>
    public Guid? ContactId { get; internal set; }
    /// <summary>The recipient correlation code.</summary>
    public string? RecipientId { get; set; }
    /// <summary>Contact salutation (Mr, Mrs etc).</summary>
    public string? Salutation { get; set; }
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The full name.</summary>
    public string? FullName { get; set; }
    /// <summary>The email.</summary>
    public string? Email { get; set; }
    /// <summary>The phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Gets or sets the list of messages.</summary>
    public List<string> Channels { get; set; } = new List<string>();
    /// <summary>Defines when the inbox message was read.</summary>
    public DateTimeOffset? CreatedOn { get; set; }
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

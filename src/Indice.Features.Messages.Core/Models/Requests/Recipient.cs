using Indice.Features.Messages.Core.Events;

namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>
/// Represents the response containing details about campaign messages, including the message content, contact
/// information, and associated channels.
/// </summary>
public class Recipient
{
    /// <summary>The unique id of the contact.</summary>
    public Contact? Contact { get; set; }
    /// <summary>Gets or sets the list of messages.</summary>
    public List<string> Channels { get; set; } = new List<string>();
    /// <summary>Defines when the inbox message was read.</summary>
    public DateTimeOffset? CreatedOn { get; set; }
}
/// <summary>
/// Represents the response containing details about the recipient and includes the message events per channels.
/// </summary>
public class RecipientMessageEvents 
{
    /// <summary>The contact information of the recepient</summary>
    public Contact Recipient { get; set; } = null!;
    /// <summary>The content of the campaign.</summary>
    public MessageContentDictionary Content { get; set; } = new MessageContentDictionary();
    /// <summary>
    /// Gets or sets the list of messages .
    /// </summary>
    public List<MessageEvent> Events { get; set; } = new List<MessageEvent>();
}

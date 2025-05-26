using System.Threading.Channels;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when sending an email.</summary>
public class SendEmailEvent
{
    /// <summary>The id of the campaign.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>The id of the contact.</summary>
    public Guid ContactId { get; set; }
    /// <summary>The title of the message.</summary>
    public string? Title { get; set; }
    /// <summary>The body of the message.</summary>
    public string? Body { get; set; }
    /// <summary>The optional message sender.</summary>
    public MessageSender? Sender { get; set; }
    /// <summary>Optional data for the campaign.</summary>
    public dynamic? Data { get; set; }
    /// <summary>Defines if push notification is sent to all registered user devices.</summary>
    public bool Broadcast { get; set; }
    /// <summary>The id of the recipient.</summary>
    public string? RecipientId { get; set; }
    /// <summary>The email of the recipient.</summary>
    public string? RecipientEmail { get; set; }
    /// <summary>The type details of the campaign.</summary>
    public MessageType? MessageType { get; set; }
    /// <summary>The message Id.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Creates a <see cref="SendEmailEvent"/> instance from a <see cref="ResolveMessageEvent"/> instance.</summary>
    /// <param name="messageEvent">The event model used when a contact is resolved from an external system.</param>
    /// <param name="contact">The resolved contact</param>
    /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
    /// <param name="messageId">The id of the message.</param>
    public static SendEmailEvent FromContactResolutionEvent(ResolveMessageEvent messageEvent, Contact contact, Guid messageId, bool broadcast) => new() {
        Body = messageEvent.Campaign!.Content[nameof(MessageChannelKind.Email)].Body,
        Sender = messageEvent.Campaign.Content[nameof(MessageChannelKind.Email)].Sender,
        Broadcast = broadcast,
        CampaignId = messageEvent.Campaign.Id,
        Data = messageEvent.Campaign.Data,
        MessageType = messageEvent.Campaign.Type,
        RecipientEmail = contact.Email,
        RecipientId = contact.RecipientId,
        Title = messageEvent.Campaign.Content[nameof(MessageChannelKind.Email)].Title,
        ContactId = contact.Id!.Value,
        MessageId = messageId
    };
    /// <summary>
    /// Converts the current <see cref="SendEmailEvent"/> to a <see cref="MessageEvent"/> with the specified type.
    /// </summary>
    /// <param name="type">the type of the event</param>
    /// <returns></returns>
    public MessageEvent ToMessageEvent(string type) => new MessageEvent {
        CampaignId = CampaignId,
        ContactId = ContactId,
        MessageId = MessageId,
        Type = type,
        Channel = MessageChannelKind.Email.ToString()
    };
}

using System.Dynamic;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Events;

/// <summary>The event model used when sending a push notification.</summary>
public class SendPushNotificationEvent
{
    /// <summary>The id of the campaign.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>The title of the message.</summary>
    public string? Title { get; set; }
    /// <summary>The body of the message.</summary>
    public string? Body { get; set; }
    /// <summary>Optional data for the campaign.</summary>
    public dynamic? Data { get; set; }
    /// <summary>Defines if push notification is sent to all registered user devices.</summary>
    public bool Broadcast { get; set; }
    /// <summary>The id of the recipient.</summary>
    public string? RecipientId { get; set; }
    /// <summary>The type details of the campaign.</summary>
    public MessageType? MessageType { get; set; }
    /// <summary>The message Id.</summary>
    public Guid? MessageId { get; set; }

    /// <summary>Creates a <see cref="SendPushNotificationEvent"/> instance from a <see cref="CampaignCreatedEvent"/> instance.</summary>
    /// <param name="campaign">Models a contact in the system as a member of a distribution list.</param>
    /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
    /// <param name="recipientId">The id of the recipient.</param>
    public static SendPushNotificationEvent FromCampaignCreatedEvent(CampaignCreatedEvent campaign, bool broadcast, string? recipientId = null) => new() {
        Body = campaign.Content[nameof(MessageChannelKind.PushNotification)].Body,
        Broadcast = broadcast,
        CampaignId = campaign.Id,
        Data = campaign.Data,
        MessageType = campaign.Type,
        RecipientId = recipientId,
        Title = campaign.Content[nameof(MessageChannelKind.PushNotification)].Title
    };

    /// <summary>Creates a <see cref="SendPushNotificationEvent"/> instance from a <see cref="ResolveMessageEvent"/> instance.</summary>
    /// <param name="event">The event model used when a contact is resolved from an external system.</param>
    /// <param name="contact">The resolved contact</param>
    /// <param name="broadcast">Defines if push notification is sent to all registered user devices.</param>
    /// <param name="messageId">The id of the message.</param>
    public static SendPushNotificationEvent FromContactResolutionEvent(ResolveMessageEvent @event, Contact contact, bool broadcast, Guid? messageId = null) => new() {
        Body = @event.Campaign!.Content[nameof(MessageChannelKind.PushNotification)].Body,
        Broadcast = broadcast,
        CampaignId = @event.Campaign.Id,
        Data = @event.Campaign.Data,
        MessageType = @event.Campaign.Type,
        RecipientId = contact.RecipientId,
        MessageId = messageId,
        Title = @event.Campaign.Content[nameof(MessageChannelKind.PushNotification)].Title
    };
}

using System.Dynamic;
using System.Text.Json;
using Indice.Features.Messages.Core.Events;
using Indice.Serialization;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="SendPushNotificationEvent"/>.</summary>
/// <remarks>Creates a new instance of <see cref="SendPushNotificationHandler"/>.</remarks>
/// <param name="pushNotificationServiceFactory">Push notification service abstraction in order to support different providers.</param>
/// <exception cref="ArgumentNullException"></exception>
public class SendPushNotificationHandler(IPushNotificationServiceFactory pushNotificationServiceFactory) : ICampaignJobHandler<SendPushNotificationEvent>
{
    private IPushNotificationServiceFactory PushNotificationServiceFactory { get; } = pushNotificationServiceFactory ?? throw new ArgumentNullException(nameof(pushNotificationServiceFactory));

    /// <summary>Sends a push notification to all users or a single one.</summary>
    /// <param name="pushNotification">The event model used when sending a push notification.</param>
    public async Task Process(SendPushNotificationEvent pushNotification) {
        ExpandoObject data = pushNotification.Data is not null && (pushNotification.Data is not string || !string.IsNullOrWhiteSpace(pushNotification.Data))
            ? JsonSerializer.Deserialize<ExpandoObject>(pushNotification.Data, JsonSerializerOptionDefaults.GetDefaultSettings())
            : new ExpandoObject();

        //Intentioanally added for naming consistency. external MessageId == internal CampaignId
        //Essentially the domain ID for messages is the internal campaign ID.
        //The internal message ID is just for data integrity.
        data.TryAdd("messageId", pushNotification.CampaignId);

        var pushNotificationService = PushNotificationServiceFactory.Create(KeyedServiceNames.PushNotificationServiceKey);
        var pushBody = pushNotification.Body ?? "-";
        if (pushNotification.Broadcast) {
            await pushNotificationService.BroadcastAsync(pushNotification.Title!, pushBody, data, pushNotification.MessageType?.Name);
        } else {
            string[]? tags = !string.IsNullOrEmpty(pushNotification.RecipientId) ? [pushNotification.RecipientId] : null;
            await pushNotificationService.SendToUserAsync(pushNotification.Title!, pushBody, data, pushNotification.RecipientId, classification: pushNotification.MessageType?.Name, tags: tags);
        }
    }
}

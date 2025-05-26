using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="SendSmsEvent"/>.</summary>
public class SendSmsHandler : ICampaignJobHandler<SendSmsEvent>
{
    /// <summary>Creates a new instance of <see cref="SendEmailHandler"/>.</summary>
    /// <param name="smsService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="campaignEventQueue">Campaign event queue abstraction.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SendSmsHandler(ISmsService smsService, CampaignEventQueue campaignEventQueue) {
        SmsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        CampaignEventQueue = campaignEventQueue;
    }

    private ISmsService SmsService { get; }
    private CampaignEventQueue CampaignEventQueue { get; }

    /// <summary>Sends a push notification to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(SendSmsEvent @event) {
        await SmsService.SendAsync(@event.RecipientPhoneNumber!, @event.Title!, @event.Body, sender: @event.Sender?.IsEmpty == false ? new SmsSender(@event.Sender.Sender!, @event.Sender.DisplayName!) : null);
        await CampaignEventQueue.EnqueueAsync(@event.ToMessageEvent(MessageEventType.Sent.ToString()));
    }
}
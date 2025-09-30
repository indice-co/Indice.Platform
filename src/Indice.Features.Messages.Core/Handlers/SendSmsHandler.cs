using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services;
using Indice.Services;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="SendSmsEvent"/>.</summary>
public class SendSmsHandler : ICampaignJobHandler<SendSmsEvent>
{
    /// <summary>Creates a new instance of <see cref="SendSmsHandler"/>.</summary>
    /// <param name="smsService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="messageEventQueue">Campaign event queue abstraction.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SendSmsHandler(ISmsService smsService, MessageEventQueue messageEventQueue) {
        SmsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        MessageEventQueue = messageEventQueue;
    }

    private ISmsService SmsService { get; }
    private MessageEventQueue MessageEventQueue { get; }

    /// <summary>Sends a push notification to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(SendSmsEvent @event) {
        await SmsService.SendAsync(@event.RecipientPhoneNumber!, @event.Title!, @event.Body, sender: @event.Sender?.IsEmpty == false ? new SmsSender(@event.Sender.Sender!, @event.Sender.DisplayName!) : null);
        await MessageEventQueue.EnqueueAsync(@event.ToMessageEvent(MessageEventType.Sent.ToString()));
    }
}
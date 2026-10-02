using System.Linq.Expressions;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Services;

/// <summary>The rules that decide which message events count as a successful send.</summary>
/// <remarks>
/// Email, SMS and push notification count on a successful <see cref="MessageEventType.Sent"/> event.
/// Inbox has no send step, so it counts on a successful <see cref="MessageEventType.Created"/> event.
/// </remarks>
public static class MessageStatsEligibility
{
    private const string Sent = nameof(MessageEventType.Sent);
    private const string Created = nameof(MessageEventType.Created);
    private const string Inbox = nameof(MessageChannelKind.Inbox);
    private const string PushNotification = nameof(MessageChannelKind.PushNotification);
    private const string Email = nameof(MessageChannelKind.Email);
    private const string Sms = nameof(MessageChannelKind.SMS);

    /// <summary>The rules of <see cref="ToDelta(MessageEvent)"/> in a form that a database query can use. Keep the two in step.</summary>
    public static readonly Expression<Func<DbMessageEvent, bool>> IsSuccessfulSend = x => x.Success && (
        (x.Type == Sent && (x.Channel == Email || x.Channel == Sms || x.Channel == PushNotification)) ||
        (x.Type == Created && x.Channel == Inbox)
    );

    /// <summary>Converts a message event to a successful send.</summary>
    /// <param name="messageEvent">The message event.</param>
    /// <returns>The successful send, or null when the event does not count.</returns>
    public static MessageStatDelta? ToDelta(MessageEvent messageEvent) {
        if (!messageEvent.Success) {
            return null;
        }
        var channel = ToChannel(messageEvent.Channel);
        var counted = channel switch {
            MessageChannelKind.None => false,
            MessageChannelKind.Inbox => messageEvent.Type == Created,
            _ => messageEvent.Type == Sent
        };
        if (!counted) {
            return null;
        }
        return new MessageStatDelta {
            CampaignId = messageEvent.CampaignId,
            Channel = channel,
            Day = DateOnly.FromDateTime(messageEvent.CreatedOn.UtcDateTime)
        };
    }

    /// <summary>Converts the channel name of a message event to a single channel.</summary>
    /// <param name="channel">The channel name, as saved on the message event.</param>
    /// <returns>The channel, or <see cref="MessageChannelKind.None"/> when the name is not one of the four channels.</returns>
    public static MessageChannelKind ToChannel(string channel) => channel switch {
        Inbox => MessageChannelKind.Inbox,
        PushNotification => MessageChannelKind.PushNotification,
        Email => MessageChannelKind.Email,
        Sms => MessageChannelKind.SMS,
        _ => MessageChannelKind.None
    };

    /// <summary>Gets the first day of the month that a day belongs to.</summary>
    /// <param name="day">The day.</param>
    public static DateOnly MonthOf(DateOnly day) => new(day.Year, day.Month, 1);
}

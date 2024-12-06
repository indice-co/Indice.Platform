namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription Response.</summary>
public class NotificationSubscriptionResponse
{
    /// <summary>User's notification subscriptions.</summary>
    public List<NotificationSubscription>? NotificationSubscriptions { get; set; }
}
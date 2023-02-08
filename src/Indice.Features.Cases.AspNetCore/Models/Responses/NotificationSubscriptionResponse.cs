namespace Indice.Features.Cases.Models.Responses;

/// <summary>
/// The notification subscription Response.
/// </summary>
public class NotificationSubscriptionResponse
{
    /// <summary>
    /// User's notification subscriptions.
    /// </summary>
    public List<NotificationSubscription> NotificationSubscriptions { get; set; }
}
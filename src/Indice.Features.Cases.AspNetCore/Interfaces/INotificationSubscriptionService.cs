using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Interface for the Case Type Notifications Subscriptions domain.
    /// </summary>
    public interface INotificationSubscriptionService
    {
        /// <summary>
        /// Get subscribers that have opted-in for notifications for their group.
        /// </summary>
        /// <param name="groupId">The Id of the group.</param>
        /// <returns></returns>
        Task<IEnumerable<NotificationSubscription>> GetSubscribersByGroupId(string groupId);

        /// <summary>
        /// Create a new notification subscription for a user and a groupId.
        /// <remarks>If a subscription already exists, this service will force delete the previous subscription.</remarks>
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        Task Subscribe(NotificationSubscription subscriber);

        /// <summary>
        /// Get the notification subscriptions for a user.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<bool> GetSubscriptions(ListOptions<NotificationFilter> options);

        /// <summary>
        /// Remove all user subscriptions.
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        Task Unsubscribe(NotificationFilter criteria);
    }
}
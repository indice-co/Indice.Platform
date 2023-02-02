using System.Security.Claims;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Interface for Notification Subscriptions domain.
    /// </summary>
    public interface INotificationSubscriptionService
    {
        /// <summary>
        /// Get the notification subscriptions for a user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<NotificationSubscriptionDTO> GetSubscriptions(ClaimsPrincipal user, ListOptions<NotificationFilter> options);

        /// <summary>
        /// Create a new notification subscription for a user and a groupId.
        /// <remarks>If a subscription already exists, this service will force delete the previous subscription.</remarks>
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        Task Subscribe(List<NotificationSubscriptionSetting> settings, NotificationSubscription subscriber);

        /// <summary>
        /// Get subscribers that have opted-in for notifications for their group.
        /// </summary>
        /// <param name="caseTypeId">The case Type Id.</param>
        /// <param name="groupId">The Id of the group.</param>
        /// <returns></returns>
        Task<IEnumerable<NotificationSubscription>> GetSubscribers(Guid caseTypeId, string groupId);
    }
}
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Interfaces
{
    public interface ICaseTypeNotificationSubscriptionService
    {
        /// <summary>
        /// Get users that have opted in for notifications for their branch
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        Task<IEnumerable<DbCaseTypeNotificationSubscription>> GetCaseTypeUsersByGroupId(string groupId);

        /// <summary>
        /// Create a new notification subscription for a user and a groupId.
        /// <remarks>If a subscription already exists, this service will force delete the previous subscription.</remarks>
        /// </summary>
        /// <param name="user">The user to create the subscription</param>
        /// <returns></returns>
        Task CreateCaseTypeNotificationSubscription(ClaimsPrincipal user);

        /// <summary>
        /// Get the notification subscriptions for a user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        Task<bool> GetCaseTypeNotificationSubscriptionByUser(ClaimsPrincipal user);

        /// <summary>
        /// Remove all user subscriptions.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        Task DeleteCaseTypeNotificationSubscriptionByUser(ClaimsPrincipal user);
    }
}
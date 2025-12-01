using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>Interface for Notification Subscriptions domain.</summary>
public interface INotificationSubscriptionService
{
    /// <summary>
    /// Retrieves a paged list of notification subscribers that match the specified filter and listing options.
    /// </summary>
    Task<ResultSet<NotificationSubscription>> GetSubscribers(ListOptions<NotificationFilter> options);

    /// <summary>
    /// Create a new notification subscription for a user and a groupId.
    /// <remarks>If a subscription already exists, this service will force delete the previous subscription.</remarks>
    /// </summary>
    /// <param name="caseTypeIds"></param>
    /// <param name="subscriber"></param>
    /// <returns></returns>
    Task Subscribe(List<Guid> caseTypeIds, NotificationSubscription subscriber);
}
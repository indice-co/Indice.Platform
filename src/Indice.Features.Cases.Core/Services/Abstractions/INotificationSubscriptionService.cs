using Indice.Features.Cases.Core.Models;
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
    /// Subscribes the specified subscriber to notifications for one or more case types.
    /// </summary>
    /// <param name="subscriber">The subscriber to register for case type notifications. Cannot be null.</param>
    /// <param name="caseTypeIds">Optional case type identifiers to subscribe to. If not specified or empty, all existing subscriptions will be removed (the subscriber will be unsubscribed from all case types).</param>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    Task Subscribe(Subscriber subscriber, params List<Guid> caseTypeIds);
}
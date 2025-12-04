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
    /// <param name="caseTypeId">The identifier of the primary case type to subscribe to.</param>
    /// <param name="otherCaseTypeIds">Optional additional case type identifiers to subscribe to. If not specified, only the primary case type is used.</param>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    Task Subscribe(Subscriber subscriber, Guid caseTypeId, params Guid[]? otherCaseTypeIds);
}

/// <summary>
/// Provides extension methods for the <see cref="INotificationSubscriptionService"/> interface to facilitate
/// notification subscription operations.
/// </summary>
public static class INotificationSubscriptionServiceExtensions
{
    /// <summary>
    /// Subscribes the specified subscriber to notifications for one or more case types.
    /// </summary>
    /// <param name="service">The notification subscription service used to register the subscriber.</param>
    /// <param name="subscriber">The subscriber to be registered for notifications.</param>
    /// <param name="caseTypeIds">An array of case type identifiers to which the subscriber will be subscribed. Must contain at least one element.</param>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="caseTypeIds"/> is null or empty.</exception>
    public static Task Subscribe(this INotificationSubscriptionService service, Subscriber subscriber, List<Guid> caseTypeIds) {
        if (caseTypeIds == null || caseTypeIds.Count == 0) {
            throw new ArgumentOutOfRangeException(nameof(caseTypeIds));
        }

        return service.Subscribe(subscriber, caseTypeIds[0], [.. caseTypeIds.Skip(1)]);
    }
}
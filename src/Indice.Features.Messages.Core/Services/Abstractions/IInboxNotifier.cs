using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>
/// Notifies interested parties (i.e. connected UI clients) in real time whenever a new inbox message becomes available.
/// </summary>
public interface IInboxNotifier
{
    /// <summary>Notifies a single user that a new inbox message was created for him.</summary>
    /// <param name="recipientId">The recipient id (the value of the configured user claim type, defaults to <i>sub</i>).</param>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task NotifyUserAsync(string recipientId, InboxMessageNotification notification, CancellationToken cancellationToken = default);

    /// <summary>Notifies all users that a new global inbox message is available.</summary>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task NotifyAllAsync(InboxMessageNotification notification, CancellationToken cancellationToken = default);
}

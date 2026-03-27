using Indice.Events;

namespace Indice.Features.ActivityLogs;

/// <summary>
/// Generic event handler for activity logging. This handler can be used to log any platform event.
/// </summary>
/// <typeparam name="TEvent">The type of event to handle. Must implement <see cref="IPlatformEvent"/>.</typeparam>
/// <remarks>Creates a new instance of <see cref="ActivityLogAdapterEventHandler{TEvent}"/>.</remarks>
/// <param name="activityLogPublisher">The activity log publisher used to publish activity log entries.</param>
/// <param name="activityLogFactory">The factory used to create activity log entries from events.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="activityLogPublisher"/> or <paramref name="activityLogFactory"/> is null.</exception>
public class ActivityLogAdapterEventHandler<TEvent>(
    IActivityLogPublisher activityLogPublisher,
    IActivityLogEventFactory activityLogFactory) : IPlatformEventHandler<TEvent>
    where TEvent : IPlatformEvent
{
    private readonly IActivityLogPublisher _activityLogPublisher = activityLogPublisher ?? throw new ArgumentNullException(nameof(activityLogPublisher));
    private readonly IActivityLogEventFactory _activityLogFactory = activityLogFactory ?? throw new ArgumentNullException(nameof(activityLogFactory));
    /// <inheritdoc />
    public Task Handle(TEvent @event, PlatformEventArgs args) {
        var activityLogEntry = _activityLogFactory.CreateFrom(@event);
        if (activityLogEntry is null) {
            return Task.CompletedTask;
        }
        return _activityLogPublisher.PublishAsync(activityLogEntry);
    }
}

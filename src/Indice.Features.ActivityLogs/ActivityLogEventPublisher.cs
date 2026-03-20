using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>IdentityServer event sink in order to persist data for a activity event.</summary>
internal class ActivityLogEventPublisher : IActivityEventPublisher
{
    private readonly ActivityLogEntryQueue _activityLogEntryQueue;
    private readonly ActivityLogEntryEnricherAggregator _activityLogEntryEnricherAggregator;

    /// <summary>Creates a new instance of <see cref="ActivityLogEventPublisher"/> class.</summary>
    /// <param name="activityLogEntryQueue"></param>
    /// <param name="activityLogEntryEnricherAggregator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ActivityLogEventPublisher(
        ActivityLogEntryQueue activityLogEntryQueue,
        ActivityLogEntryEnricherAggregator activityLogEntryEnricherAggregator
    ) {
        _activityLogEntryQueue = activityLogEntryQueue ?? throw new ArgumentNullException(nameof(activityLogEntryQueue));
        _activityLogEntryEnricherAggregator = activityLogEntryEnricherAggregator ?? throw new ArgumentNullException(nameof(activityLogEntryEnricherAggregator));
    }


    /// <inheritdoc />
    public async Task PublishAsync(ActivityLogEntry entry) {
        if (entry is null) {
            return;
        }
        await _activityLogEntryEnricherAggregator.EnrichAsync(entry, ActivityLogEnricherRunType.Synchronous);
        await _activityLogEntryQueue.EnqueueAsync(entry);
    }
}
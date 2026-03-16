using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>IdentityServer event sink in order to persist data for a activity event.</summary>
internal class ActivityLogEventPublisher : IActivityEventPublisher
{
    private readonly ActivityLogEntryQueue _ActivityLogEntryQueue;
    private readonly ActivityLogEntryEnricherAggregator _ActivityLogEntryEnricherAggregator;

    /// <summary>Creates a new instance of <see cref="ActivityLogEventPublisher"/> class.</summary>
    /// <param name="ActivityLogEntryQueue"></param>
    /// <param name="ActivityLogEntryEnricherAggregator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ActivityLogEventPublisher(
        ActivityLogEntryQueue ActivityLogEntryQueue,
        ActivityLogEntryEnricherAggregator ActivityLogEntryEnricherAggregator
    ) {
        _ActivityLogEntryQueue = ActivityLogEntryQueue ?? throw new ArgumentNullException(nameof(ActivityLogEntryQueue));
        _ActivityLogEntryEnricherAggregator = ActivityLogEntryEnricherAggregator;
    }


    /// <inheritdoc />
    public async Task PublishAsync(ActivityLogEntry entry) {
        if (entry is null) {
            return;
        }
        await _ActivityLogEntryEnricherAggregator.EnrichAsync(entry, ActivityLogEnricherRunType.Synchronous);
        await _ActivityLogEntryQueue.EnqueueAsync(entry);
    }
}
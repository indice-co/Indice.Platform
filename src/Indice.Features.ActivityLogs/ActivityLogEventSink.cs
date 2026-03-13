#if NET9_0_OR_GREATER
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Events;
using IdentityServer4.Services;
#endif
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>IdentityServer event sink in order to persist data for a activity event.</summary>
internal class ActivityLogEventSink : IEventSink
{
    private readonly ActivityLogEntryQueue _ActivityLogEntryQueue;
    private readonly ActivityLogEntryEnricherAggregator _ActivityLogEntryEnricherAggregator;

    /// <summary>Creates a new instance of <see cref="ActivityLogEventSink"/> class.</summary>
    /// <param name="ActivityLogEntryQueue"></param>
    /// <param name="ActivityLogEntryEnricherAggregator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ActivityLogEventSink(
        ActivityLogEntryQueue ActivityLogEntryQueue,
        ActivityLogEntryEnricherAggregator ActivityLogEntryEnricherAggregator
    ) {
        _ActivityLogEntryQueue = ActivityLogEntryQueue ?? throw new ArgumentNullException(nameof(ActivityLogEntryQueue));
        _ActivityLogEntryEnricherAggregator = ActivityLogEntryEnricherAggregator;
    }

    /// <inheritdoc />
    public async Task PersistAsync(Event @event) {
        var logEntry = ActivityLogEntryAdapterFactory.Create(@event);
        if (logEntry is null) {
            return;
        }
        await _ActivityLogEntryEnricherAggregator.EnrichAsync(logEntry, ActivityLogEnricherRunType.Synchronous);
        await _ActivityLogEntryQueue.EnqueueAsync(logEntry);
    }
}

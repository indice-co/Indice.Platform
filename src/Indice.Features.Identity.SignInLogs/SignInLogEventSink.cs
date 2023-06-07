using IdentityServer4.Events;
using IdentityServer4.Services;
using Indice.Features.Identity.SignInLogs.Enrichers;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>IdentityServer event sink in order to persist data for a sign in event.</summary>
internal class SignInLogEventSink : IEventSink
{
    private readonly SignInLogEntryQueue _signInLogEntryQueue;
    private readonly SignInLogEntryEnricherAggregator _signInLogEntryEnricherAggregator;

    /// <summary>Creates a new instance of <see cref="SignInLogEventSink"/> class.</summary>
    /// <param name="signInLogEntryQueue"></param>
    /// <param name="signInLogEntryEnricherAggregator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SignInLogEventSink(
        SignInLogEntryQueue signInLogEntryQueue,
        SignInLogEntryEnricherAggregator signInLogEntryEnricherAggregator
    ) {
        _signInLogEntryQueue = signInLogEntryQueue ?? throw new ArgumentNullException(nameof(signInLogEntryQueue));
        _signInLogEntryEnricherAggregator = signInLogEntryEnricherAggregator;
    }

    /// <inheritdoc />
    public async Task PersistAsync(Event @event) {
        var logEntry = SignInLogEntryAdapterFactory.Create(@event);
        if (logEntry is null) {
            return;
        }
        await _signInLogEntryEnricherAggregator.EnrichAsync(logEntry, SignInLogEnricherRunType.Synchronous);
        await _signInLogEntryQueue.EnqueueAsync(logEntry);
    }
}

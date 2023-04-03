using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class SignInLogEntryEnricherAggregator
{
    private IEnumerable<ISignInLogEntryEnricher> _enrichers;
    private readonly IEnumerable<ISignInLogEntryFilter> _filters;

    public SignInLogEntryEnricherAggregator(
        IEnumerable<ISignInLogEntryEnricher> enrichers,
        IEnumerable<ISignInLogEntryFilter> filters
    ) {
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        _filters = filters ?? throw new ArgumentNullException(nameof(filters));
    }

    public async Task<bool> EnrichAsync(SignInLogEntry logEntry, EnricherDependencyType? dependencyType = null) {
        var discard = false;
        if (logEntry is null) {
            return true;
        }
        discard = await MustDiscardAsync(logEntry);
        if (discard) {
            return discard;
        }
        if (dependencyType.HasValue) {
            _enrichers = _enrichers.Where(enricher => dependencyType.Value.HasFlag(enricher.DependencyType));
        }
        foreach (var enricher in _enrichers) {
            await enricher.EnrichAsync(logEntry);
        }
        return false;
    }

    private async Task<bool> MustDiscardAsync(SignInLogEntry logEntry) {
        var discard = false;
        foreach (var filter in _filters) {
            discard = await filter.Discard(logEntry);
            // If one of the filters dictates that we must discard the log entry then do not proceed with other filters.
            if (discard) {
                break;
            }
        }
        return discard;
    }
}

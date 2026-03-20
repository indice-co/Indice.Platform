using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Enrichers;

internal class ActivityLogEntryEnricherAggregator
{
    private IEnumerable<IActivityLogEntryEnricher> _enrichers;
    private readonly IEnumerable<IActivityLogEntryFilter> _filters;

    public ActivityLogEntryEnricherAggregator(
        IEnumerable<IActivityLogEntryEnricher> enrichers,
        IEnumerable<IActivityLogEntryFilter> filters
    ) {
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        _filters = filters ?? throw new ArgumentNullException(nameof(filters));
    }

    public async Task<EnrichResult> EnrichAsync(ActivityLogEntry logEntry, ActivityLogEnricherRunType? dependencyType = null) {
        var discard = false;
        if (logEntry is null) {
            return EnrichResult.Failed;
        }
        discard = await MustDiscardAsync(logEntry);
        if (discard) {
            return EnrichResult.MustDiscard;
        }
        if (dependencyType.HasValue) {
            _enrichers = _enrichers.Where(enricher => dependencyType.Value.HasFlag(enricher.RunType));
        }
        foreach (var enricher in _enrichers.OrderBy(x => x.Order)) {
            try {
                await enricher.EnrichAsync(logEntry);
            }
            catch (Exception ex) {
                throw;
            }
            
        }
        return EnrichResult.Success;
    }

    private async Task<bool> MustDiscardAsync(ActivityLogEntry logEntry) {
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

internal class EnrichResult
{
    private static readonly EnrichResult _success = new() { Succeeded = true };
    private static readonly EnrichResult _failed = new();
    private static readonly EnrichResult _mustDiscard = new() { IsDiscarded = true };

    public bool Succeeded { get; protected set; }
    public bool IsDiscarded { get; protected set; }
    public static EnrichResult Success => _success;
    public static EnrichResult Failed => _failed;
    public static EnrichResult MustDiscard => _mustDiscard;

}
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Enrichers;

internal class ActivityLogEntryEnricherAggregator
{
    private readonly IEnumerable<IActivityLogEntryEnricher> _enrichers;
    private readonly IEnumerable<IActivityLogEntryFilter> _filters;

    public ActivityLogEntryEnricherAggregator(
        IEnumerable<IActivityLogEntryEnricher> enrichers,
        IEnumerable<IActivityLogEntryFilter> filters
    ) {
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        _filters = filters ?? throw new ArgumentNullException(nameof(filters));
    }

    public async Task<EnrichResult> EnrichAsync(ActivityLogEntry logEntry, ActivityLogEnricherRunType? dependencyType = null) {
        if (logEntry is null) {
            return EnrichResult.Failed;
        }
        // Pre-enrichment filters discard on data already set by the converter (e.g. Category), before any enrichment work.
        if (await MustDiscardAsync(logEntry, ActivityLogFilterPhase.PreEnrichment)) {
            return EnrichResult.MustDiscard;
        }
        var enrichersToRun = _enrichers; // Local copy
        if (dependencyType.HasValue) {
            enrichersToRun = enrichersToRun.Where(enricher => dependencyType.Value.HasFlag(enricher.RunType));
        }
        foreach (var enricher in enrichersToRun.OrderBy(x => x.Order)) {
            await enricher.EnrichAsync(logEntry);
        }
        // Post-enrichment filters discard on enriched data (e.g. a SubjectId populated by an enricher).
        if (await MustDiscardAsync(logEntry, ActivityLogFilterPhase.PostEnrichment)) {
            return EnrichResult.MustDiscard;
        }
        return EnrichResult.Success;
    }

    private async Task<bool> MustDiscardAsync(ActivityLogEntry logEntry, ActivityLogFilterPhase phase) {
        foreach (var filter in _filters.Where(filter => filter.Phase == phase)) {
            // If one of the filters dictates that we must discard the log entry then do not proceed with other filters.
            if (await filter.Discard(logEntry)) {
                return true;
            }
        }
        return false;
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
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>A filter that decides whether an <see cref="ActivityLogEntry"/> should be discarded before it is persisted.</summary>
public interface IActivityLogEntryFilter
{
    /// <summary>When this filter runs relative to enrichment. Defaults to <see cref="ActivityLogFilterPhase.PostEnrichment"/> so the filter sees the fully-enriched entry.</summary>
    ActivityLogFilterPhase Phase => ActivityLogFilterPhase.PostEnrichment;
    /// <summary>Determines whether the given <paramref name="logEntry"/> must be discarded (<see langword="true"/> drops the entry).</summary>
    /// <param name="logEntry">The activity log entry under evaluation.</param>
    Task<bool> Discard(ActivityLogEntry logEntry);
}

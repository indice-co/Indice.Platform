using Indice.Features.ActivityLogs;

namespace Indice.Features.ActivityLogs.Models;

/// <summary>Determines when an <see cref="IActivityLogEntryFilter"/> runs relative to enrichment.</summary>
public enum ActivityLogFilterPhase
{
    /// <summary>Runs before enrichers — discard on data already set by the converter (e.g. <see cref="ActivityLogEntry.Category"/>). Cheaper, as it avoids enrichment work for discarded entries.</summary>
    PreEnrichment,
    /// <summary>Runs after enrichers — discard on enriched data (e.g. a <see cref="ActivityLogEntry.SubjectId"/> populated by an enricher).</summary>
    PostEnrichment
}

using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.Identity.Server.ActivityLog;
/// <summary>
/// Discard events that do not have a subject
/// </summary>
public sealed class SubjectFilter : IActivityLogEntryFilter
{
    // Runs after enrichment so the subject set by the converter or UserInfoEnricher is taken into account.
    /// <inheritdoc />
    public ActivityLogFilterPhase Phase => ActivityLogFilterPhase.PostEnrichment;

    /// <inheritdoc />
    public Task<bool> Discard(ActivityLogEntry logEntry) =>
        Task.FromResult(string.IsNullOrWhiteSpace(logEntry?.SubjectId));
}

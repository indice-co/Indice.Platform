using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>An abstraction used to describe the implementation of a service that enriches the <see cref="ActivityLogEntry"/> class.</summary>
public interface IActivityLogEntryEnricher
{
    /// <summary>The precedence order that the enricher runs.</summary>
    public int Order { get; }
    /// <summary>The run type.</summary>
    public ActivityLogEnricherRunType RunType { get; }
    /// <summary>Enrich the <see cref="ActivityLogEntry"/> class.</summary>
    /// <param name="logEntry">The instance of <see cref="ActivityLogEntry"/> to enrich.</param>
    ValueTask EnrichAsync(ActivityLogEntry logEntry);
}
using Indice.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>Factory that creates activity log entries from platform events.</summary>
public interface IActivityLogEventFactory
{
    /// <summary>Creates an activity log entry from the specified event.</summary>
    ActivityLogEntry? CreateFrom(IPlatformEvent @event);
}
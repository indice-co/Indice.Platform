using Indice.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>Converter that creates activity log entries from platform events.</summary>
public interface IActivityLogFromEventConverter
{
    /// <summary>Creates an activity log entry from the specified event.</summary>
    ActivityLogEntry? Convert(IPlatformEvent @event);
}
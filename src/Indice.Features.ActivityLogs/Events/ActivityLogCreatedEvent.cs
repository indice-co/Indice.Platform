using Indice.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Events;

/// <summary>An event that is raised when a new <see cref="ActivityLogEntry"/> is created.</summary>
public class ActivityLogCreatedEvent : IPlatformEvent
{
    /// <summary>Creates a new instance of <see cref="ActivityLogCreatedEvent"/> class.</summary>
    /// <param name="activityLog">The log entry that was created.</param>
    public ActivityLogCreatedEvent(ActivityLogEntry activityLog) {
        ActivityLog = activityLog;
    }

    /// <summary>The log entry that was created.</summary>
    public ActivityLogEntry ActivityLog { get; set; }
}

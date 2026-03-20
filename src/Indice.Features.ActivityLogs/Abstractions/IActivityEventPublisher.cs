using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>
/// Activity event publisher that is responsible for publishing the activity log entry to the underlying mechanism.
/// </summary>
public interface IActivityEventPublisher
{
    /// <summary>
    /// Passes the activity log entry to the publisher for further processing.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    Task PublishAsync(ActivityLogEntry entry);
}

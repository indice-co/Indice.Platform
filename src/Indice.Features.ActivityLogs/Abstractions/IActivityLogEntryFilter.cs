using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

public interface IActivityLogEntryFilter
{
    Task<bool> Discard(ActivityLogEntry logEntry);
}

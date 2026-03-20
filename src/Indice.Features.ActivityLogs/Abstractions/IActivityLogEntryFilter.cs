using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

internal interface IActivityLogEntryFilter
{
    Task<bool> Discard(ActivityLogEntry logEntry);
}

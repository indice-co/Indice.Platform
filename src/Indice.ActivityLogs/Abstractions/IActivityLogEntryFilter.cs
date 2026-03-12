using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Abstractions;

internal interface IActivityLogEntryFilter
{
    Task<bool> Discard(ActivityLogEntry logEntry);
}

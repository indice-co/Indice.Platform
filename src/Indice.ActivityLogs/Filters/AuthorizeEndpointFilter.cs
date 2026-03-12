using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs.Filters;

internal class AuthorizeEndpointFilter : IActivityLogEntryFilter
{
    public Task<bool> Discard(ActivityLogEntry logEntry) => Task.FromResult("Authorize".Equals(logEntry?.ResourceId, StringComparison.OrdinalIgnoreCase));
}

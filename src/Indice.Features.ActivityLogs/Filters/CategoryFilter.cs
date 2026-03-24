using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.Filters;

internal class CategoryFilter : IActivityLogEntryFilter
{
    private readonly ActivityLogOptions _activityLogOptions;

    public CategoryFilter(IOptions<ActivityLogOptions> activityLogOptions) {
        _activityLogOptions = activityLogOptions.Value ?? throw new ArgumentNullException(nameof(activityLogOptions));
    }
    public Task<bool> Discard(ActivityLogEntry logEntry) {
        if(_activityLogOptions.Categories.Count == 0) {
            return Task.FromResult(false);
        }
        if(string.IsNullOrWhiteSpace(logEntry?.Category)) {
            return Task.FromResult(false);
        }
        if (_activityLogOptions.Categories.Contains(logEntry.Category)) { 
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}

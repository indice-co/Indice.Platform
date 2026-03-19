using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.Filters;

internal class CategoryFilter : IActivityLogEntryFilter
{
    private readonly ActivityLogOptions activityLogOptions;

    public CategoryFilter(IOptions<ActivityLogOptions> activityLogOptions) {
        this.activityLogOptions = activityLogOptions.Value ?? throw new ArgumentNullException(nameof(activityLogOptions));
    }
    public Task<bool> Discard(ActivityLogEntry logEntry) {
        if (activityLogOptions.Categories.Contains(logEntry.Category)) { 
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}

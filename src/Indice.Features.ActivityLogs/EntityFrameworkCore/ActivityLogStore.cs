using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.EntityFrameworkCore;

/// <summary>An implementation of <see cref="IActivityLogStore"/>, using Entity Framework Core as a persistence mechanism.</summary>
internal class ActivityLogStore : IActivityLogStore
{
    private readonly ActivityLogDbContext _dbContext;
    private readonly ActivityLogOptions _activityLogOptions;

    /// <summary>Creates a new instance of <see cref="ActivityLogStore"/> class.</summary>
    /// <param name="dbContext">The <see cref="ActivityLogDbContext"/> passing the configured options.</param>
    /// <param name="activityLogOptions">Options for configuring the IdentityServer activity logs mechanism.</param>
    public ActivityLogStore(
        ActivityLogDbContext dbContext,
        IOptions<ActivityLogOptions> activityLogOptions
    ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _activityLogOptions = activityLogOptions?.Value ?? throw new ArgumentNullException(nameof(activityLogOptions));
    }

    /// <inheritdoc />
    public async Task<int> Cleanup(CancellationToken cancellationToken = default) {
        var query = _dbContext
            .ActivityLogs
            .Where(x => EF.Functions.DateDiffDay(x.CreatedAt, DateTimeOffset.UtcNow) > _activityLogOptions.Cleanup.RetentionDays)
            .Take(_activityLogOptions.Cleanup.BatchSize);
        return await query.ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CreateAsync(ActivityLogEntry logEntry, CancellationToken cancellationToken = default) =>
        CreateManyAsync(new List<ActivityLogEntry> { logEntry }, cancellationToken);

    /// <inheritdoc />
    public async Task CreateManyAsync(IEnumerable<ActivityLogEntry> logEntries, CancellationToken cancellationToken = default) {
        _dbContext.ActivityLogs.AddRange(logEntries.ToDbActivityLogEntries());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResultSet<ActivityLogEntry>> ListAsync(ListOptions options, ActivityLogEntryFilter filter , CancellationToken cancellationToken = default) {
        IQueryable<Data.DbActivityLogEntry> query = _dbContext.ActivityLogs;
        if (filter is not null) {
            if (filter.From.HasValue) {
                query = query.Where(log => log.CreatedAt >= filter.From.Value);
            }
            if (filter.To.HasValue) {
                query = query.Where(log => log.CreatedAt < filter.To.Value.AddDays(1));
            }
            if (filter.Succeeded.HasValue) {
                query = query.Where(log => log.Succeeded == filter.Succeeded.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Subject)) {
                query = query.Where(log => log.SubjectId == filter.Subject || log.SubjectName == filter.Subject);
            }
            if (!string.IsNullOrWhiteSpace(filter.ActionName)) {
                query = query.Where(log => log.ActionName == filter.ActionName);
            }
            if (!string.IsNullOrWhiteSpace(filter.ApplicationId)) {
                query = query.Where(log => log.ApplicationId == filter.ApplicationId);
            }
            if (!string.IsNullOrWhiteSpace(filter.SessionId)) {
                query = query.Where(log => log.SessionId == filter.SessionId);
            }
            if (filter.MarkForReview.HasValue) {
                query = query.Where(log => log.Review == filter.MarkForReview.Value);
            }
        }
        if (string.IsNullOrWhiteSpace(options?.Sort)) {
            query = query.OrderByDescending(log => log.CreatedAt);
        }
        return await query.Select(ObjectMapping.ToActivityLogEntry).ToResultSetAsync(options, cancellationToken);
    }

    public async Task<int> UpdateAsync(Guid id, ActivityLogEntryRequest model, CancellationToken cancellationToken = default) {
        var query = _dbContext.ActivityLogs.Where(x => x.Id == id);
        return await query.ExecuteUpdateAsync(updates => updates.SetProperty(x => x.Review, model.Review), cancellationToken);
    }

}

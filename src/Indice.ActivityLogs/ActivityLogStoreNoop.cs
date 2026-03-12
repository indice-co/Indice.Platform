using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Indice.Types;

namespace Indice.Features.ActivityLogs;

/// <summary>Default implementation of <see cref="IActivityLogStore"/> that has no functionality.</summary>
public class ActivityLogStoreNoop : IActivityLogStore
{
    /// <inheritdoc />
    public Task<int> Cleanup(CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public Task CreateAsync(ActivityLogEntry logEntry, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task CreateManyAsync(IEnumerable<ActivityLogEntry> logEntries, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<ResultSet<ActivityLogEntry>> ListAsync(ListOptions options, ActivityLogEntryFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(new ResultSet<ActivityLogEntry>(Enumerable.Empty<ActivityLogEntry>(), 0));

    /// <inheritdoc />
    public Task<int> UpdateAsync(Guid id, ActivityLogEntryRequest model, CancellationToken cancellationToken = default) => Task.FromResult(0);
}

using Indice.Features.ActivityLogs.Models;
using Indice.Types;

namespace Indice.Features.ActivityLogs.Abstractions;

/// <summary>A service that contains operations used to persist the data of a user's activity event.</summary>
public interface IActivityLogStore
{
    /// <summary>Performs a deletion on log entries base on the <see cref="LogCleanupOptions"/>.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The number of rows deleted.</returns>
    Task<int> Cleanup(CancellationToken cancellationToken = default);
    /// <summary>Records a user's activity event.</summary>
    /// <param name="logEntry">The data of a user's activity event to be recorded.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task CreateAsync(ActivityLogEntry logEntry, CancellationToken cancellationToken = default);
    /// <summary>Records a user's multiple activity events.</summary>
    /// <param name="logEntries">The data of a user's activity event to be recorded.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task CreateManyAsync(IEnumerable<ActivityLogEntry> logEntries, CancellationToken cancellationToken = default);
    /// <summary>Queries a list of user's activity event data, applying the provided filter.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter">The filter options to apply.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task<ResultSet<ActivityLogEntry>> ListAsync(ListOptions options, ActivityLogEntryFilter filter, CancellationToken cancellationToken = default);
    /// <summary>Updates the specified log entry.</summary>
    /// <param name="id">The unique identifier of the log entry.</param>
    /// <param name="model">Request model for updating a <see cref="ActivityLogEntry"/> instance.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The number of rows updated.</returns>
    Task<int> UpdateAsync(Guid id, ActivityLogEntryRequest model, CancellationToken cancellationToken = default);
}

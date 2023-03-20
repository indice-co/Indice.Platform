using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Abstractions;

/// <summary>A service that contains operations used to persist the data of a user's sign in event.</summary>
public interface ISignInLogStore
{
    /// <summary>Performs a deletion on log entries base on the <see cref="LogCleanupOptions"/>.</summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The number of rows deleted.</returns>
    Task<int> Cleanup(CancellationToken cancellationToken = default);
    /// <summary>Records a user's sign in event.</summary>
    /// <param name="logEntry">The data of a user's sign in event to be recorded.</param>
    Task CreateAsync(SignInLogEntry logEntry);
    /// <summary>Queries a list of user's sign in event data, applying the provided filter.</summary>
    /// <param name="options">The filter options to apply.</param>
    Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions options);
    /// <summary>Updates the specified log entry.</summary>
    /// <param name="id">The unique identifier of the log entry.</param>
    /// <param name="model">Request model for updating a <see cref="SignInLogEntry"/> instance.</param>
    /// <returns>The number of rows updated.</returns>
    Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model);
}

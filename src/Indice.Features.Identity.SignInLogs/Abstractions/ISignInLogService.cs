using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Abstractions;

/// <summary>A service that contains operations used to persist the data of a user's sign in event.</summary>
public interface ISignInLogService
{
    /// <summary>Records a user's sign in event.</summary>
    /// <param name="logEntry">The data of a user's sign in event to be recorded.</param>
    Task CreateAsync(SignInLogEntry logEntry);
    /// <summary>Queries a list of user's sign in event data, applying the provided filter.</summary>
    /// <param name="options">The filter options to apply.</param>
    Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions options);
    /// <summary>Updates the </summary>
    /// <param name="id">The unique identifier of the log entry.</param>
    /// <param name="model">Request model for updating a <see cref="SignInLogEntry"/> instance.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model);
}

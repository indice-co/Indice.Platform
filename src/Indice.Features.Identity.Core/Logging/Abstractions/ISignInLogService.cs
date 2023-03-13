using Indice.Features.Identity.Core.Logging.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.Logging.Abstractions;

/// <summary>A service that contains operations used to persist the data of a user's sign in event.</summary>
public interface ISignInLogService
{
    /// <summary>Records a user's sign in event.</summary>
    /// <param name="logEntry">The data of a user's sign in event to be recorded.</param>
    Task CreateAsync(SignInLogEntry logEntry);
    /// <summary>Queries a list of user's sign in event data, applying the provided filter.</summary>
    /// <param name="options">The filter options to apply.</param>
    Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions<SignInLogEntryFilter> options);
}

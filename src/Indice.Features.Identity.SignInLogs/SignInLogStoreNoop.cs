using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Default implementation of <see cref="ISignInLogStore"/> that has no functionality.</summary>
public class SignInLogStoreNoop : ISignInLogStore
{
    /// <inheritdoc />
    public Task<int> Cleanup(CancellationToken cancellationToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public Task CreateAsync(SignInLogEntry logEntry, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task CreateManyAsync(IEnumerable<SignInLogEntry> logEntries, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions<SignInLogEntryFilter> options, CancellationToken cancellationToken = default) => Task.FromResult(new ResultSet<SignInLogEntry>(Enumerable.Empty<SignInLogEntry>(), 0));

    /// <inheritdoc />
    public Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model, CancellationToken cancellationToken = default) => Task.FromResult(0);
}

using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>Default implementation of <see cref="ISignInLogService"/> that has no functionality.</summary>
public class SignInLogServiceNoop : ISignInLogService
{
    /// <inheritdoc />
    public Task CreateAsync(SignInLogEntry auditEntry) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions options) => Task.FromResult(new ResultSet<SignInLogEntry>(Enumerable.Empty<SignInLogEntry>(), 0));

    /// <inheritdoc />
    public Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model) => Task.FromResult(0);
}

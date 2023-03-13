using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.Logging;

/// <summary>Default implementation of <see cref="ISignInLogService"/> that has no functionality.</summary>
public class SignInLogServiceNoop : ISignInLogService
{
    /// <inheritdoc />
    public Task CreateAsync(SignInLogEntry auditEntry) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions<SignInLogEntryFilter> options) => Task.FromResult(new ResultSet<SignInLogEntry>(Enumerable.Empty<SignInLogEntry>(), 0));
}

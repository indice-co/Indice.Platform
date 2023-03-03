using System;
using System.Threading;
using System.Threading.Tasks;
using Indice.Types;

namespace Indice.Services;

/// <summary>
/// A <see cref="ILockManager"/> implementation that does nothing.
/// </summary>
public class LockManagerNoop : ILockManager
{
    /// <inheritdoc/>
    public Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null, CancellationToken cancellationToken = default) => 
        Task.FromResult((ILockLease)new LockLease(new Base64Id(Guid.NewGuid()).ToString(), name, this));

    /// <inheritdoc/>
    public Task Cleanup() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ReleaseLock(ILockLease @lock) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ILockLease> Renew(string name, string leaseId, CancellationToken cancellationToken = default) => Task.FromResult((ILockLease)new LockLease(leaseId, name, this));
}

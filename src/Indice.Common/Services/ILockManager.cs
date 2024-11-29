using Polly;

namespace Indice.Services;

/// <summary>Provides an abstraction for operations that help manage an exclusive lock on a distributed environment.</summary>
/// <remarks>
/// If you need to acquire a theoretical background, i would suggest to read the following:
/// https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html
/// https://pdfs.semanticscholar.org/a25e/ee836dbd2a5ae680f835309a484c9f39ae4e.pdf
/// </remarks>
public interface ILockManager
{
    /// <summary>Acquire a lock or throws.</summary>
    /// <param name="name">Topic or name.</param>
    /// <param name="duration">The duration the lease will be active. Defaults 30 seconds.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <exception cref="LockManagerException">Occurs when the lock cannot be acquired.</exception>
    Task<ILockLease> AcquireLock(string name, TimeSpan? duration = null, CancellationToken cancellationToken = default);
    /// <summary>Renews an existing lease by lease id. This extends the duration of a non expired existing lease.</summary>
    /// <param name="name">Topic or name.</param>
    /// <param name="leaseId">The lease Id (different on every lock).</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns></returns>
    /// <remarks>In case a lease has expired the client can renew the lease with their lease ID immediately if the blob has not been modified.</remarks>
    Task<ILockLease> Renew(string name, string leaseId, CancellationToken cancellationToken = default);
    /// <summary>Releases a lock.</summary>
    /// <param name="lock">The lock lease.</param>
    Task ReleaseLock(ILockLease @lock);
    /// <summary>Clean up any expired lock leases. Depends on implementation may not be needed if the lock leases are self mainained like in Azure Blob Storage.</summary>
    Task Cleanup();
}

/// <summary>Lock lease abstraction.</summary>
public interface ILockLease : IDisposable, IAsyncDisposable
{
    /// <summary>The lease Id (different on every lock).</summary>
    string LeaseId { get; }
    /// <summary>The subject of the lock (name).</summary>
    string Name { get; }
    /// <summary>Try renew the existing lease period.</summary>
    public Task RenewAsync();
}

/// <summary>This object represents a lock lease.</summary>
public class LockLease : ILockLease
{
    private bool _disposed = false;

    /// <summary>Constructs a lock lease.</summary>
    /// <param name="leaseId">The lease Id (different on every lock).</param>
    /// <param name="name">The subject of the lock (name).</param>
    /// <param name="manager">The lock manager.</param>
    public LockLease(string leaseId, string name, ILockManager manager) {
        LeaseId = leaseId ?? throw new ArgumentNullException(nameof(leaseId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LockManager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <inheritdoc />
    public string LeaseId { get; }
    /// <inheritdoc />
    public string Name { get; }
    /// <summary>The lock manager.</summary>
    public ILockManager LockManager { get; }

    /// <inheritdoc />
    public void Dispose() {
        if (!_disposed) {
            LockManager.ReleaseLock(this)
                       .GetAwaiter()
                       .GetResult();
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (!_disposed) {
            await LockManager.ReleaseLock(this);
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public async Task RenewAsync() => await LockManager.Renew(Name, LeaseId);
}

/// <summary>A result object representing the lock operation result on the <see cref="ILockManager"/>.</summary>
public class LockLeaseResult
{
    private LockLeaseResult(ILockLease? @lock, bool ok) {
        Lock = @lock;
        Ok = ok;
    }

    /// <summary>Success or fail indicator.</summary>
    public bool Ok { get; }
    /// <summary>The lock itself. It is <see cref="IDisposable"/></summary>
    public ILockLease? Lock { get; }

    /// <summary>Successful <see cref="LockLeaseResult"/> factory.</summary>
    /// <param name="lock">The lock</param>
    public static LockLeaseResult Success(ILockLease @lock) => new(@lock, true);

    /// <summary>Failed  <see cref="LockLeaseResult"/> factory.</summary>
    public static LockLeaseResult Fail() => new(null, false);
}

/// <summary>Extension methods on <see cref="ILockManager"/> type.</summary>
public static class ILockManagerExtensions
{
    /// <summary>
    /// Try acquire the lock. If success it will return a successful <see cref="LockLeaseResult"/>.
    /// If the <see cref="ILockManager"/> throws a <seealso cref="LockManagerException"/> it will catch that and return a failed <see cref="LockLeaseResult"/>.
    /// </summary>
    /// <param name="manager">The instance of <see cref="ILockManager"/>.</param>
    /// <param name="name">Topic or name.</param>
    /// <param name="duration">The duration the lease will be active. Defaults 30 seconds.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The task the represent the asynchronous operation result for acquiring the lock.</returns>
    public static async Task<LockLeaseResult> TryAcquireLock(this ILockManager manager, string name, TimeSpan? duration = null, CancellationToken cancellationToken = default) {
        try {
            var @lock = await manager.AcquireLock(name, duration, cancellationToken);
            return LockLeaseResult.Success(@lock);
        } catch (LockManagerException) {
            return LockLeaseResult.Fail();
        }
    }

    /// <summary>
    /// Try acquire the lock. If success it will return a successful <see cref="LockLeaseResult"/>.
    /// If the <see cref="ILockManager"/> throws a <seealso cref="LockManagerException"/> it will catch that and return a failed <see cref="LockLeaseResult"/>.
    /// In case of failure it will retry sometimes before calling it a day.
    /// </summary>
    /// <param name="manager">The instance of <see cref="ILockManager"/>.</param>
    /// <param name="name">Topic or name.</param>
    /// <param name="retryCount">Specifies the number of retries to perform.</param>
    /// <param name="retryAfter">Specifies the duration in seconds to wait for a particular retry attempt.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The task the represent the asynchronous operation result for acquiring the lock.</returns>
    public static async Task<LockLeaseResult> TryAcquireLockWithRetryPolicy(this ILockManager manager, string name, int retryCount = 3, int retryAfter = 3, CancellationToken cancellationToken = default) {
        var policy = Policy.HandleResult<LockLeaseResult>(lockLeaseResult => !lockLeaseResult.Ok)
                           .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryAfter));
        return await policy.ExecuteAsync(token => manager.TryAcquireLock(name), cancellationToken);
    }

    /// <summary>Tries to acquire the lock indefinitely until the action succeeds.</summary>
    /// <param name="manager">The instance of <see cref="ILockManager"/>.</param>
    /// <param name="name">Topic or name.</param>
    /// <param name="retryAfter">Specifies the duration in seconds to wait for a particular retry attempt.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The task that represents the asynchronous operation result for acquiring the lock.</returns>
    public static async Task<LockLeaseResult> TryAcquireLockForeverPolicy(this ILockManager manager, string name, int retryAfter = 3, CancellationToken cancellationToken = default) {
        var policy = Policy.HandleResult<LockLeaseResult>(lockLeaseResult => !lockLeaseResult.Ok)
                           .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(retryAfter));
        return await policy.ExecuteAsync(token => manager.TryAcquireLock(name), cancellationToken);
    }

    /// <summary>Tries to renew the lock lease.</summary>
    /// <param name="lockLease">The lock lease.</param>
    /// <returns>True if operation is successful, otherwise false.</returns>
    public static async Task<bool> TryRenewAsync(this ILockLease lockLease) {
        try {
            await lockLease.RenewAsync();
            return true;
        } catch (Exception) {
            return false;
        }
    }

    /// <summary>Tries to renew the lock lease.</summary>
    /// <param name="lockLease">The lock lease.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <param name="intervalInSeconds">Specifies the duration in seconds to wait for a renew attempt.</param>
    /// <returns>The task that represents the asynchronous operation result for renewing the lease.</returns>
    public static async Task TryRenewUntilCancelled(this ILockLease lockLease, CancellationToken cancellationToken, int intervalInSeconds = 10) {
        while (!cancellationToken.IsCancellationRequested) {
            // Immediately try to renew the lease.
            var renewed = await lockLease.TryRenewAsync();
            if (!renewed) {
                return;
            }
            // Wait for an amount of time before trying again to renew the lease.
            await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds), cancellationToken);
        }
    }

    /// <summary>Uses a lease on a shared resource imposed by the <see cref="ILockManager"/> (i.e an Azure Storage blob or a shared file in a NFS) to provide a mechanism for implementing a shared, distributed mutex.</summary>
    /// <remarks>
    /// This mutex can be used to elect a leader among a group of role instances in an Azure cloud service or an on-premise infrastructure. 
    /// The first role instance to acquire the lease is elected the leader, and remains the leader until it releases the lease or isn't able to renew the lease.
    /// </remarks>
    /// <param name="manager">The instance of <see cref="ILockManager"/>.</param>
    /// <param name="task">A task that references the code that the role instance should run if it successfully acquires the lease over the blob and is elected the leader.</param>
    /// <param name="taskName">A name for the task to run.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <param name="options">Options for configuring the ExclusiveRun method.</param>
    /// <returns>The task that represents the asynchronous operation result for running the specified delegate.</returns>
    public static async Task ExclusiveRun(this ILockManager manager, string taskName, Func<CancellationToken, Task> task, CancellationToken cancellationToken, ExclusiveRunOptions options) {
        // Run in a while loop as long as cancellation has not been requested for the provided token.
        CancellationTokenSource? linkedTokenSource = null;
        while (!(linkedTokenSource?.IsCancellationRequested ?? false)) {
            // Try to acquire the lock.
            var lockResult = await manager.TryAcquireLock(taskName, duration: TimeSpan.FromSeconds(options.LockDuration), cancellationToken: cancellationToken);
            // If it is not possible to acquire the lock, wait for 30 seconds and try again. Being a leader requires patience and consistency :)
            if (!lockResult.Ok) {
                if (options.RetryIntervalInSeconds.HasValue) {
                    await Task.Delay(TimeSpan.FromSeconds(options.RetryIntervalInSeconds.Value), cancellationToken);
                    continue;
                } else {
                    break;
                }
            }
            // Create a new linked cancellation token source, so if either the original token is canceled or the lease cannot be renewed, then the leader task can be canceled.
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // The role instance that is executing the following code is now the leader.
            using (lockResult.Lock) {
                // The leader task.
                var leaderTask = task.Invoke(linkedTokenSource.Token);
                // The renew lease task.
                var renewLeaseTask = TryRenewUntilCancelled(lockResult.Lock!, linkedTokenSource.Token, intervalInSeconds: (int)Math.Round(options.LockDuration * (2M / 3)));
                // Wait for either of the two tasks to complete.
                await Task.WhenAny(leaderTask, renewLeaseTask);
            }
            // Cancel the user's leader task or the renew lease task, as the current role instance is no longer the leader.
            linkedTokenSource.Cancel();
        }
        linkedTokenSource?.Dispose();
    }

    /// <summary>Uses a lease on a shared resource imposed by the <see cref="ILockManager"/> (i.e an Azure Storage blob or a shared file in a NFS) to provide a mechanism for implementing a shared, distributed mutex.</summary>
    /// <remarks>
    /// This mutex can be used to elect a leader among a group of role instances in an Azure cloud service or an on-premise infrastructure. 
    /// The first role instance to acquire the lease is elected the leader, and remains the leader until it releases the lease or isn't able to renew the lease.
    /// </remarks>
    /// <param name="manager">The instance of <see cref="ILockManager"/>.</param>
    /// <param name="task">A task that references the code that the role instance should run if it successfully acquires the lease over the blob and is elected the leader.</param>
    /// <param name="taskName">A name for the task to run.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The task that represents the asynchronous operation result for running the specified delegate.</returns>
    public static Task ExclusiveRun(this ILockManager manager, string taskName, Func<CancellationToken, Task> task, CancellationToken cancellationToken) => 
        manager.ExclusiveRun(taskName, task, cancellationToken, ExclusiveRunOptions.Default());
}

/// <summary>Exception thrown when the <see cref="ILockManager"/> could not acquire a lock.</summary>
public class LockManagerException : Exception
{
    /// <summary>Constructs a new <see cref="LockManagerException"/>.</summary>
    /// <param name="lockName">the name of the lock</param>
    public LockManagerException(string lockName) : base($"Could not acquire lock '{lockName}'.") { }
    /// <summary>Constructs a new <see cref="LockManagerException"/>.</summary>
    /// <param name="lockName">The name of the lock.</param>
    /// <param name="innerException">The inner exception.</param>
    public LockManagerException(string lockName, Exception innerException) : base($"Could not acquire lock '{lockName}'.", innerException) { }
}

/// <summary>Options for configuring the ExclusiveRun method.</summary>
public class ExclusiveRunOptions
{
    private int _lockDurationInSeconds = DEFAULT_LOCK_DURATION_IN_SECONDS;
    private int? _retryIntervalInSeconds = null;
    /// <summary>The minimum duration that a lock leases lasts.</summary>
    public const int MIN_LOCK_DURATION_IN_SECONDS = 3;
    /// <summary>The maximum duration that a lock leases lasts.</summary>
    public const int MAX_LOCK_DURATION_IN_SECONDS = 120;
    /// <summary>The default duration that a lock leases lasts.</summary>
    public const int DEFAULT_LOCK_DURATION_IN_SECONDS = 30;

    /// <summary>
    /// The duration that a lock lease lasts.<br/>
    /// Min duration is 3 seconds and max is 120.<br/>
    /// Any value used outside these boundaries is reset to min or max value correspondingly.<br/>
    /// Defaults to 30 seconds.
    /// </summary>
    public int LockDuration {
        get => _lockDurationInSeconds;
        set {
            _lockDurationInSeconds = value;
            if (value < MIN_LOCK_DURATION_IN_SECONDS) {
                _lockDurationInSeconds = MIN_LOCK_DURATION_IN_SECONDS;
            } else if (value > MAX_LOCK_DURATION_IN_SECONDS) {
                _lockDurationInSeconds = MAX_LOCK_DURATION_IN_SECONDS;
            }
        }
    }

    /// <summary>
    /// Specifies the retry interval in seconds that is used to acquire the lock.<br/>
    /// If not set the task exits immediately and the role instance is not trying to become the leader.<br/>
    /// If set to a value less or equal to zero, the value is reset to 1.
    /// </summary>
    public int? RetryIntervalInSeconds {
        get => _retryIntervalInSeconds;
        set { 
            _retryIntervalInSeconds = value;
            if (value <= 0) {
                _retryIntervalInSeconds = 1;
            }
        }
    }

    /// <summary>Gets the default configuration for <see cref="ExclusiveRunOptions"/>.</summary>
    public static ExclusiveRunOptions Default() => new() { 
        LockDuration = DEFAULT_LOCK_DURATION_IN_SECONDS,
        _retryIntervalInSeconds = null
    };
}

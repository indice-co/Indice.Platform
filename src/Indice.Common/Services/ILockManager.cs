using System;
using System.Threading.Tasks;
using Polly;

namespace Indice.Services
{
    /// <summary>
    /// If you need to acquire a theoritical background, i would suggest to read the following
    /// https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html
    /// https://pdfs.semanticscholar.org/a25e/ee836dbd2a5ae680f835309a484c9f39ae4e.pdf
    /// </summary>
    public interface ILockManager
    {
        /// <summary>
        /// Aquire a lock or throws
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeout"></param>
        /// <exception cref="LockManagerLockException">Occures when the lock cannot be aquired</exception>
        /// <returns></returns>
        Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null);
        /// <summary>
        /// Releases a lock
        /// </summary>
        /// <param name="lock"></param>
        /// <returns></returns>
        Task ReleaseLock(ILockLease @lock);

        /// <summary>
        /// Clean up any expired lock leases. 
        /// Depends on implementation may not be needed if the lock leases are self mainained like in azure blob storage.
        /// </summary>
        /// <returns></returns>
        Task Cleanup();
    }

    /// <summary>
    /// Lcok lease abstraction.
    /// </summary>
    public interface ILockLease : IDisposable
    {
        /// <summary>
        /// The lease Id (different on every lock)
        /// </summary>
        string LeaseId { get; }
        /// <summary>
        /// The subject of the lock (name).
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// This object represents a lock lease.
    /// </summary>
    public class LockLease : ILockLease
    {
        private bool _disposed = false;

        /// <summary>
        /// Constructs a lock lease.
        /// </summary>
        /// <param name="leaseId">The lease Id (different on every lock)</param>
        /// <param name="name">The subject of the lock (name).</param>
        /// <param name="manager">The lease manager</param>
        public LockLease(string leaseId, string name, ILockManager manager) {
            LeaseId = leaseId ?? throw new ArgumentNullException(nameof(leaseId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <inheritdoc />
        public string LeaseId { get; }
        /// <inheritdoc />
        public string Name { get; }
        /// <summary>
        /// The lock manager is used internally to release the lock.
        /// </summary>
        public ILockManager Manager { get; }

        /// <inheritdoc />
        public void Dispose() {
            if (!_disposed) {
                Manager.ReleaseLock(this);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// A result object repesenting the lock operation result on the <see cref="ILockManager"/>
    /// </summary>
    public class LockLeaseResult
    {
        private LockLeaseResult(ILockLease @lock, bool ok) {
            Lock = @lock;
            Ok = ok;
        }

        /// <summary>
        /// Success/Fail indicator.
        /// </summary>
        public bool Ok { get; }
        /// <summary>
        /// The lock itsef. It is <see cref="IDisposable"/>
        /// </summary>
        public ILockLease Lock { get; }

        /// <summary>
        /// Successful <see cref="LockLeaseResult"/> factory.
        /// </summary>
        /// <param name="lock">The lock</param>
        /// <returns></returns>
        public static LockLeaseResult Success(ILockLease @lock) => new LockLeaseResult(@lock, true);

        /// <summary>
        /// Failed  <see cref="LockLeaseResult"/> factory.
        /// </summary>
        /// <returns></returns>
        public static LockLeaseResult Fail() => new LockLeaseResult(null, false);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class LockLeaseExtensions
    {
        /// <summary>
        /// Try Aquire the lock. If success it will return a successful <see cref="LockLeaseResult"/>.
        /// If the lockmanger throws a <seealso cref="LockManagerLockException"/> it will catch that and return a failed <see cref="LockLeaseResult"/>
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<LockLeaseResult> TryAquireLock(this ILockManager manager, string name) {
            try {
                var @lock = await manager.AcquireLock(name);
                return LockLeaseResult.Success(@lock);
            } catch (LockManagerLockException) {
                return LockLeaseResult.Fail();
            }
        }

        /// <summary>
        /// Try Aquire the lock. If success it will return a successful <see cref="LockLeaseResult"/>.
        /// If the lockmanger throws a <seealso cref="LockManagerLockException"/> it will catch that and return a failed <see cref="LockLeaseResult"/>.
        /// In case of failure it will retry sometimes before calling it a day.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryAfter"></param>
        /// <returns></returns>
        public static async Task<LockLeaseResult> TryAquireLockWithRetryPolicy(this ILockManager manager, string name, int retryCount = 3, int retryAfter = 3) {
            var policy = Policy.HandleResult<LockLeaseResult>(lockLeaseResult => !lockLeaseResult.Ok)
                               .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryAfter));
            return await policy.ExecuteAsync(async () => await manager.TryAquireLock(name));
        }
    }

    /// <summary>
    /// Exception thrown when the lockmanager could not aquire a lock.
    /// </summary>
    public class LockManagerLockException : Exception
    {
        /// <summary>
        /// Contructs a new <see cref="LockManagerLockException"/>
        /// </summary>
        /// <param name="lockName">the name of the lock</param>
        public LockManagerLockException(string lockName) : base($"Could not aquire lock {lockName}") {

        }
    }
}
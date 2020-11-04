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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lock"></param>
        /// <returns></returns>
        Task ReleaseLock(ILockLease @lock);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ILockLease : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        string LeaseId { get; }
        /// <summary>
        /// 
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LockLease : ILockLease
    {
        private bool _disposed = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leaseId"></param>
        /// <param name="name"></param>
        /// <param name="manager"></param>
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
        /// 
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
    /// 
    /// </summary>
    public class LockLeaseResult
    {
        private LockLeaseResult(ILockLease @lock, bool ok) {
            Lock = @lock;
            Ok = ok;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Ok { get; }
        /// <summary>
        /// 
        /// </summary>
        public ILockLease Lock { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lock"></param>
        /// <returns></returns>
        public static LockLeaseResult Success(ILockLease @lock) => new LockLeaseResult(@lock, true);

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<LockLeaseResult> TryAquireLock(this ILockManager manager, string name) {
            try {
                var @lock = await manager.AcquireLock(name);
                return LockLeaseResult.Success(@lock);
            } catch {
                return LockLeaseResult.Fail();
            }
        }

        /// <summary>
        /// 
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
}

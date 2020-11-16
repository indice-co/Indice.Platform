using System;
using System.Threading;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.Extensions.Logging;

namespace Indice.Services
{
    /// <summary>
    /// Implementation of <see cref="ILockManager"/>.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/dotnet/api/system.threading.monitor</remarks>
    public class LockManagerInMemory : ILockManager
    {
        private readonly ILogger<LockManagerInMemory> _logger;
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public LockManagerInMemory(ILogger<LockManagerInMemory> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _signal.Release();
        }

        /// <inheritdoc />
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            await _signal.WaitAsync();
            var leaseId = new Base64Id(Guid.NewGuid()).ToString();
            _logger.LogInformation("Item with lease id {0} acquired the lock.", leaseId);
            return new LockLease(leaseId, name, this);
        }

        /// <inheritdoc />
        public Task Cleanup() {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ReleaseLock(ILockLease @lock) {
            _signal.Release();
            _logger.LogInformation("Item with lease id {0} released the lock.", @lock.LeaseId);
            return Task.CompletedTask;
        }
    }
}

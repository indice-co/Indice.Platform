using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Entity framework lockmanager implementation.
    /// </summary>
    public class EFLockManager : ILockManager
    {
        private readonly TaskDbContext _DbContext;

        /// <summary>
        /// Constructs the <see cref="EFLockManager"/>
        /// </summary>
        /// <param name="dbContext"></param>
        public EFLockManager(TaskDbContext dbContext) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc/>
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            var @lock = new DbLock { Id = Guid.NewGuid(), Name = name, ExrirationDate = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30)) };
            var success = false;
            _DbContext.Locks.Add(@lock);
            try {
                await _DbContext.SaveChangesAsync();
                success = true;
            } catch (DbUpdateException) {
                var oldlock = await _DbContext.Locks.Where(x => x.Name == name && x.ExrirationDate <= DateTime.UtcNow).SingleOrDefaultAsync();
                if (oldlock != null) {
                    _DbContext.Locks.Remove(oldlock);
                    _DbContext.Locks.Add(@lock);
                    await _DbContext.SaveChangesAsync();
                    success = true;
                }
            }
            if (!success)
                throw new Exception($"Unable to aquire lease {name}");
            return new LockLease(new Base64Id(@lock.Id), name, this);
        }

        /// <inheritdoc/>
        public async Task ReleaseLock(ILockLease @lock) {
            var oldlock = await _DbContext.Locks.Where(x => x.Name == @lock.Name && x.Id == Base64Id.Parse(@lock.LeaseId).Id).SingleOrDefaultAsync();
            if (oldlock != null) {
                _DbContext.Locks.Remove(oldlock);
                await _DbContext.SaveChangesAsync();
            }
        }
    }
}

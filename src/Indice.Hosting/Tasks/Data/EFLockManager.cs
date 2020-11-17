using System;
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
        public EFLockManager(LockDbContext dbContext) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc/>
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            var @lock = new DbLock { Id = Guid.NewGuid(), Name = name, ExpirationDate = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30)) };
            var success = false;
            try {
                //_DbContext.Locks.Add(@lock);
                //await _DbContext.SaveChangesAsync();
                var query = @"INSERT INTO [work].[Lock] ([Id], [Name], [ExpirationDate]) VALUES ({0}, {1}, {2});";
                await _DbContext.Database.ExecuteSqlRawAsync(query, @lock.Id, @lock.Name, @lock.ExpirationDate);
                success = true;
            } catch (Microsoft.Data.SqlClient.SqlException) {
                await Cleanup();
                success = false;
            }
            if (!success)
                throw new LockManagerLockException($"Unable to aquire lease {name}");
            return new LockLease(new Base64Id(@lock.Id), name, this);
        }

        /// <inheritdoc/>
        public async Task ReleaseLock(ILockLease @lock) {
            var query = @"DELETE FROM [work].[Lock] WHERE ([Name] = {0} AND [Id] < {1}) OR [ExpirationDate] < GetDate();";
            await _DbContext.Database.ExecuteSqlRawAsync(query, @lock.Name, Base64Id.Parse(@lock.LeaseId).Id);
        }

        /// <inheritdoc/>
        public async Task Cleanup() {
            var query = @"DELETE FROM [work].[Lock] Where [ExpirationDate] < GetDate();";
            await _DbContext.Database.ExecuteSqlRawAsync(query);
        }
    }
}

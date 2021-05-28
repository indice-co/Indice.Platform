using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Indice.Hosting.EntityFrameworkCore;
using Indice.Services;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.SqlClient
{
    /// <summary>
    /// SQL Server <see cref="ILockManager"/> implementation.
    /// </summary>
    public class SqlServerLockManager : ILockManager
    {
        private readonly TaskDbContext _dbContext;

        /// <summary>
        /// Constructs the <see cref="SqlServerLockManager"/>
        /// </summary>
        /// <param name="dbContext"></param>
        public SqlServerLockManager(LockDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc/>
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            var duration= timeout ?? TimeSpan.FromSeconds(30);
            var @lock = new DbLock {
                Id = Guid.NewGuid(),
                Name = name,
                ExpirationDate = DateTime.UtcNow.Add(duration),
                Duration = (int)duration.TotalSeconds
            };
            bool success;
            try {
                var query = @"INSERT INTO [work].[Lock] ([Id], [Name], [ExpirationDate], [Duration]) VALUES ({0}, {1}, {2}, {3});";
                await _dbContext.Database.ExecuteSqlRawAsync(query, @lock.Id, @lock.Name, @lock.ExpirationDate, @lock.Duration);
                success = true;
            } catch (SqlException) {
                await Cleanup();
                success = false;
            }
            if (!success) {
                throw new LockManagerException($"Unable to aquire lease {name}.");
            }
            return new LockLease(new Base64Id(@lock.Id), name, this);
        }

        /// <inheritdoc/>
        public async Task ReleaseLock(ILockLease @lock) {
            var query = @"DELETE FROM [work].[Lock] WHERE ([Name] = {0} AND [Id] < {1}) OR [ExpirationDate] < GetDate();";
            await _dbContext.Database.ExecuteSqlRawAsync(query, @lock.Name, Base64Id.Parse(@lock.LeaseId).Id);
        }

        /// <inheritdoc/>
        public async Task<ILockLease> Renew(string name, string leaseId) {
            var base64Id = Base64Id.Parse(leaseId);
            bool success;
            try {
                var query = @"UPDATE [work].[Lock] SET [ExpirationDate] = DATEADD(second, [Duration], GETDATE()) WHERE [Id] = {0};";
                var affecterRows = await _dbContext.Database.ExecuteSqlRawAsync(query, base64Id.Id);
                success = affecterRows > 0;
            } catch (SqlException) {
                await Cleanup();
                success = false;
            }
            if (!success) {
                throw new LockManagerException($"Unable to renew lease {name} for leaseid {leaseId}.");
            }
            return new LockLease(base64Id, name, this);
        }

        /// <inheritdoc/>
        public async Task Cleanup() {
            var query = @"DELETE FROM [work].[Lock] Where [ExpirationDate] < GetDate();";
            await _dbContext.Database.ExecuteSqlRawAsync(query);
        }
    }
}

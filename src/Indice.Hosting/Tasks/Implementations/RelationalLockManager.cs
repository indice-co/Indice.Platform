using System;
using System.Threading.Tasks;
using Indice.Hosting.Tasks.Data;
using Indice.Hosting.Tasks.Data.Models;
using Indice.Services;
using Indice.Types;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indice.Hosting.Tasks.Implementations
{
    /// <summary>
    /// <see cref="ILockManager"/> implementation for a relational database.
    /// </summary>
    public class RelationalLockManager : ILockManager
    {
        private readonly TaskDbContext _dbContext;
        private readonly LockManagerQueryDescriptor _queryDescriptor;

        /// <summary>
        /// Constructs the <see cref="RelationalLockManager"/>.
        /// </summary>
        /// <param name="dbContext">Contains the required tables to implement a locking mechanism using a relational database.</param>
        public RelationalLockManager(LockDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _queryDescriptor = new LockManagerQueryDescriptor(dbContext);
        }

        /// <inheritdoc/>
        public async Task<ILockLease> AcquireLock(string name, TimeSpan? timeout = null) {
            var duration = timeout ?? TimeSpan.FromSeconds(30);
            var @lock = new DbLock {
                Id = Guid.NewGuid(),
                Name = name,
                ExpirationDate = DateTime.UtcNow.Add(duration),
                Duration = (int)duration.TotalSeconds
            };
            bool success;
            try {
                await _dbContext.Database.ExecuteSqlRawAsync(_queryDescriptor.AcquireLock, @lock.Id, @lock.Name, @lock.ExpirationDate, @lock.Duration);
                success = true;
            } catch (Exception exception) when (exception is SqlException || exception is NpgsqlException) {
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
            await _dbContext.Database.ExecuteSqlRawAsync(_queryDescriptor.ReleaseLock, @lock.Name, Base64Id.Parse(@lock.LeaseId).Id);
        }

        /// <inheritdoc/>
        public async Task<ILockLease> Renew(string name, string leaseId) {
            var base64Id = Base64Id.Parse(leaseId);
            bool success;
            try {
                var affecterRows = await _dbContext.Database.ExecuteSqlRawAsync(_queryDescriptor.RenewLease, base64Id.Id);
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
            await _dbContext.Database.ExecuteSqlRawAsync(_queryDescriptor.Cleanup);
        }
    }

    internal class LockManagerQueryDescriptor
    {
        public LockManagerQueryDescriptor(DbContext context) {
            switch (context.Database.ProviderName) {
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    AcquireLock = PostgreSqlLockManagerQueries.AcquireLock;
                    ReleaseLock = PostgreSqlLockManagerQueries.ReleaseLock;
                    RenewLease = PostgreSqlLockManagerQueries.RenewLease;
                    Cleanup = PostgreSqlLockManagerQueries.Cleanup;
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                default:
                    AcquireLock = SqlServerLockManagerQueries.AcquireLock;
                    ReleaseLock = SqlServerLockManagerQueries.ReleaseLock;
                    RenewLease = SqlServerLockManagerQueries.RenewLease;
                    Cleanup = SqlServerLockManagerQueries.Cleanup;
                    break;
            }
        }

        public string AcquireLock { get; }
        public string ReleaseLock { get; }
        public string RenewLease { get; }
        public string Cleanup { get; }
    }

    internal static class SqlServerLockManagerQueries
    {
        public const string AcquireLock = @"
            INSERT INTO [work].[Lock] ([Id], [Name], [ExpirationDate], [Duration]) 
            VALUES ({0}, {1}, {2}, {3});";
        public const string ReleaseLock = @"
            DELETE FROM [work].[Lock] 
            WHERE ([Name] = {0} AND [Id] < {1}) OR [ExpirationDate] < GETDATE();";
        public const string RenewLease = @"
            UPDATE [work].[Lock] 
            SET [ExpirationDate] = DATEADD(second, [Duration], GETDATE()) 
            WHERE [Id] = {0}";
        public const string Cleanup = @"
            DELETE FROM [work].[Lock] 
            WHERE [ExpirationDate] < GETDATE();";
    }

    internal static class PostgreSqlLockManagerQueries
    {
        public const string AcquireLock = @"
            INSERT INTO ""work"".""Lock"" (""Id"", ""Name"", ""ExpirationDate"", ""Duration"") 
            VALUES ({0}, {1}, {2}, {3});";
        public const string ReleaseLock = @"
            DELETE FROM ""work"".""Lock"" 
            WHERE (""Name"" = {0} AND ""Id"" < {1}) OR ""ExpirationDate"" < NOW();";
        public const string RenewLease = @"
            UPDATE ""work"".""Lock"" 
            SET ""ExpirationDate"" = NOW() + ""Duration"" * INTERVAL '1 second'; 
            WHERE ""Id"" = {0}";
        public const string Cleanup = @"
            DELETE FROM ""work"".""Lock"" 
            WHERE ""ExpirationDate"" < NOW();";
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Hosting.Data;
using Indice.Hosting.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Postgres
{
    /// <summary>
    /// An implementation of <see cref="IMessageQueue{T}"/> for PostgreSQL.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PostgresMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly TaskDbContext _dbContext;
        private readonly IQueueNameResolver<T> _queueNameResolver;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Constructs a new <see cref="PostgresMessageQueue{T}"/>.
        /// </summary>
        /// <param name="dbContext">A <see cref="DbContext"/> for hosting multiple <see cref="IMessageQueue{T}"/>.</param>
        /// <param name="queueNameResolver">Resolves the queue name.</param>
        /// <param name="workerJsonOptions">These are the options regarding json Serialization. They are used internally for persisting payloads.</param>
        public PostgresMessageQueue(TaskDbContext dbContext, IQueueNameResolver<T> queueNameResolver, WorkerJsonOptions workerJsonOptions) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _queueNameResolver = queueNameResolver ?? throw new ArgumentNullException(nameof(queueNameResolver));
            _jsonSerializerOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /* We do not need to implement this method here, since when an item is dequeued it is also removed at the same time. */
        /// <inheritdoc/>
        public Task Cleanup(int? batchSize = null) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<int> Count() {
            var query = @"
                SELECT COUNT(*) 
                FROM ""work"".""QMessage""
                WHERE ""QueueName"" = '@QueueName';
            ";
            var dbConnection = _dbContext.Database.GetDbConnection();
            dbConnection.Open();
            using var command = dbConnection.CreateCommand();
            command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(), DbType.String);
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            var count = command.ExecuteScalar();
            return Task.FromResult((int)count);
        }

        /// <inheritdoc/>
        public Task<QMessage<T>> Dequeue() {
            var query = @"
                DELETE FROM ""work"".""QMessage""
                USING(
                    SELECT *
                    FROM ""work"".""QMessage""
                    WHERE ""QueueName"" = @QueueName
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                ) q
                WHERE q.""Id"" = ""work"".""QMessage"".""Id""
                RETURNING ""work"".""QMessage"".*;
            ";
            var dbConnection = _dbContext.Database.GetDbConnection();
            dbConnection.Open();
            using var command = dbConnection.CreateCommand();
            command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(), DbType.String);
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            using var dataReader = command.ExecuteReader();
            DbQMessage message = null;
            while (dataReader.Read()) {
                message = new DbQMessage {
                    Id = dataReader.GetGuid(0),
                    QueueName = dataReader.IsDBNull(1) ? default : dataReader.GetString(1),
                    Payload = dataReader.IsDBNull(2) ? default : dataReader.GetString(2),
                    Date = dataReader.GetDateTime(3),
                    RowVersion = dataReader.IsDBNull(4) ? default : (byte[])dataReader[nameof(DbQMessage.RowVersion)],
                    DequeueCount = dataReader.GetInt32(5),
                    State = (QMessageState)dataReader.GetInt32(6)
                };
            }
            return Task.FromResult(message != null ? message.ToModel<T>(_jsonSerializerOptions) : default);
        }

        /// <inheritdoc/>
        public Task Enqueue(QMessage<T> item, bool isPoison) {
            var query = @"
                INSERT INTO ""work"".""QMessage"" (""Id"", ""QueueName"", ""Payload"", ""Date"", ""DequeueCount"", ""State"")
                VALUES (@Id, @QueueName, @Payload, @Date, @DequeueCount, 0);
            ";
            var dbConnection = _dbContext.Database.GetDbConnection();
            dbConnection.Open();
            using var command = dbConnection.CreateCommand();
            command.AddParameterWithValue("@Id", item.Id, DbType.Guid);
            command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(isPoison), DbType.String);
            command.AddParameterWithValue("@Payload", JsonSerializer.Serialize(item.Value, _jsonSerializerOptions), DbType.String);
            command.AddParameterWithValue("@Date", item.Date, DbType.DateTime2);
            command.AddParameterWithValue("@DequeueCount", item.DequeueCount, DbType.Int32);
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            var rowsAffected = command.ExecuteNonQuery();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task EnqueueRange(IEnumerable<T> items) {
            var initialQuery = @"INSERT INTO ""work"".""QMessage"" (""Id"", ""QueueName"", ""Payload"", ""Date"", ""DequeueCount"", ""State"") VALUES";
            var query = initialQuery;
            var dbConnection = _dbContext.Database.GetDbConnection();
            dbConnection.Open();
            const double maxBatchSize = 1000d;
            var batches = Math.Ceiling(items.Count() / maxBatchSize);
            for (var i = 0; i < batches; i++) {
                var remainingItemsCount = items.Count() - (i * maxBatchSize);
                var iterationLength = remainingItemsCount >= maxBatchSize ? maxBatchSize : remainingItemsCount;
                using var command = dbConnection.CreateCommand();
                for (var j = 0; j < iterationLength; j++) {
                    query += $"(MD5(RANDOM()::TEXT || CLOCK_TIMESTAMP()::TEXT)::UUID, @QueueName{j}, @Payload{j}, NOW(), 0, 0)";
                    var isLastItem = j == iterationLength - 1;
                    query += !isLastItem ? ", " : ";";
                    command.AddParameterWithValue($"@QueueName{j}", _queueNameResolver.Resolve(), DbType.String);
                    command.AddParameterWithValue($"@Payload{j}", JsonSerializer.Serialize(items.ElementAt(j), _jsonSerializerOptions), DbType.String);
                }
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                var rowsAffected = command.ExecuteNonQuery();
                query = initialQuery;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<T> Peek() {
            var query = @"
                SELECT *
                FROM ""work"".""QMessage""
                LIMIT 1
                FOR UPDATE SKIP LOCKED;
            ";
            var dbConnection = _dbContext.Database.GetDbConnection();
            dbConnection.Open();
            using var command = dbConnection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            using var dataReader = command.ExecuteReader();
            string payload = null;
            while (dataReader.Read()) {
                payload = dataReader.IsDBNull(0) ? default : dataReader.GetString(2);
            }
            return Task.FromResult(!string.IsNullOrEmpty(payload) ? JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions) : default);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Hosting.EntityFrameworkCore;

namespace Indice.Hosting.SqlClient
{
    /// <summary>
    /// An implementation of <see cref="IMessageQueue{T}"/> for SQL Server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlServerMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IQueueNameResolver<T> _queueNameResolver;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Constructs a new <see cref="SqlServerMessageQueue{T}"/>.
        /// </summary>
        /// <param name="dbConnectionFactory">A factory class that generates instances of type <see cref="IDbConnection"/>.</param>
        /// <param name="queueNameResolver">Resolves the queue name.</param>
        /// <param name="workerJsonOptions">These are the options regarding json Serialization. They are used internally for persisting payloads.</param>
        public SqlServerMessageQueue(IDbConnectionFactory dbConnectionFactory, IQueueNameResolver<T> queueNameResolver, WorkerJsonOptions workerJsonOptions) {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _queueNameResolver = queueNameResolver ?? throw new ArgumentNullException(nameof(queueNameResolver));
            _jsonSerializerOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        // We do not need to implement this method here, since when an item is dequeued it is also removed at the same time.
        /// <inheritdoc/>
        public Task Cleanup(int? batchSize = null) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<int> Count() {
            var sql = @"
                SELECT COUNT(*)
                FROM [work].[QMessage]
                WHERE [QueueName] = @QueueName;
            ";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(), DbType.String);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                var count = command.ExecuteScalar();
                return Task.FromResult((int)count);
            }
        }

        /// <inheritdoc/>
        public Task<QMessage<T>> Dequeue() {
            var sql = @"
                SET NOCOUNT ON; 
                WITH cte AS (
                    SELECT TOP(1) * 
                    FROM [work].[QMessage] WITH (ROWLOCK, READPAST)
                    WHERE [QueueName] = @QueueName
                    ORDER BY [Date] ASC
                )
                DELETE FROM cte 
                OUTPUT [deleted].*;
            ";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(), DbType.String);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                var dataReader = command.ExecuteReader();
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
        }

        /// <inheritdoc/>
        public Task Enqueue(QMessage<T> item, bool isPoison) {
            var sql = @"
                INSERT INTO [work].[QMessage] ([Id], [QueueName], [Payload], [Date], [DequeueCount], [State]) 
                VALUES (@Id, @QueueName, @Payload, @Date, @DequeueCount, 0);
            ";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.AddParameterWithValue("@Id", item.Id, DbType.Guid);
                command.AddParameterWithValue("@QueueName", _queueNameResolver.Resolve(isPoison), DbType.String);
                command.AddParameterWithValue("@Payload", JsonSerializer.Serialize(item.Value, _jsonSerializerOptions), DbType.String);
                command.AddParameterWithValue("@Date", item.Date, DbType.DateTime2);
                command.AddParameterWithValue("@DequeueCount", item.DequeueCount, DbType.Int32);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                var rowsAffected = command.ExecuteNonQuery();
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task EnqueueRange(IEnumerable<T> items) {
            var initialSql = "INSERT INTO [work].[QMessage] ([Id], [QueueName], [Payload], [Date], [DequeueCount], [State]) VALUES";
            var sql = initialSql;
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                const double maxBatchSize = 1000d;
                var batches = Math.Ceiling(items.Count() / maxBatchSize);
                for (var i = 0; i < batches; i++) {
                    var remainingItemsCount = items.Count() - (i * maxBatchSize);
                    var iterationLength = remainingItemsCount >= maxBatchSize ? maxBatchSize : remainingItemsCount;
                    var command = dbConnection.CreateCommand();
                    for (var j = 0; j < iterationLength; j++) {
                        sql += $"(NEWID(), @QueueName{j}, @Payload{j}, GETUTCDATE(), 0, 0)";
                        var isLastItem = j == iterationLength - 1;
                        sql += !isLastItem ? ", " : ";";
                        command.AddParameterWithValue($"@QueueName{j}", _queueNameResolver.Resolve(), DbType.String);
                        command.AddParameterWithValue($"@Payload{j}", JsonSerializer.Serialize(items.ElementAt(j), _jsonSerializerOptions), DbType.String);
                    }
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    var rowsAffected = command.ExecuteNonQuery();
                    sql = initialSql;
                }
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task<T> Peek() {
            var sql = @"
                SELECT TOP(1) [Payload] 
                FROM[work].[QMessage] WITH (ROWLOCK, READPAST) 
                ORDER BY [Date] ASC;
            ";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                var dataReader = command.ExecuteReader();
                string payload = null;
                while (dataReader.Read()) {
                    payload = dataReader.IsDBNull(0) ? default : dataReader.GetString(2);
                }
                return Task.FromResult(!string.IsNullOrEmpty(payload) ? JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions) : default);
            }
        }
    }
}

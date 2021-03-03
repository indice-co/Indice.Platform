using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// An implementation of <see cref="IMessageQueue{T}"/> for SQL Server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlServerMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly string _queueName;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbConnectionFactory"></param>
        /// <param name="queueNameResolver"></param>
        /// <param name="workerJsonOptions"></param>
        public SqlServerMessageQueue(IDbConnectionFactory dbConnectionFactory, IQueueNameResolver<T> queueNameResolver, WorkerJsonOptions workerJsonOptions) {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _queueName = queueNameResolver?.Resolve() ?? throw new ArgumentNullException(nameof(queueNameResolver));
            _jsonSerializerOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /// <inheritdoc/>
        public Task Cleanup(int? batchSize = null) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<int> Count() {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<QMessage<T>> Dequeue() {
            var sql = "SET NOCOUNT ON; " +
                      "WITH cte AS (" +
                          "SELECT TOP(1) * " +
                          "FROM[work].[QMessage] WITH (ROWLOCK, READPAST) " +
                          "ORDER BY [Date] ASC" +
                      ")" +
                      "DELETE FROM cte " +
                      "OUTPUT [deleted].*";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
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
        public Task Enqueue(T item, Guid? messageId, bool isPoison) {
            var sql = "INSERT INTO [work].[QMessage] ([Id], [QueueName], [Payload], [Date], [DequeueCount], [State]) " +
                      "VALUES (NEWID(), @QueueName, @Payload, GETUTCDATE(), 0, 0)";
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.AddParameterWithValue("@QueueName", _queueName, DbType.String);
                command.AddParameterWithValue("@Payload", JsonSerializer.Serialize(item, _jsonSerializerOptions), DbType.String);
                command.CommandText = sql;
                command.CommandType = CommandType.Text;
                var rowsAffected = command.ExecuteNonQuery();
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task EnqueueRange(IEnumerable<T> items) {
            var initialSql = @"INSERT INTO [work].[QMessage] ([Id], [QueueName], [Payload], [Date], [DequeueCount], [State]) VALUES";
            var sql = initialSql;
            using (var dbConnection = _dbConnectionFactory.Create()) {
                dbConnection.Open();
                const double maxBatchSize = 1000d;
                var batches = Math.Ceiling(items.Count() / maxBatchSize);
                for (var i = 0; i < batches; i++) {
                    var remainingItems = items.Count() - (i * maxBatchSize);
                    var iterationLength = remainingItems >= maxBatchSize ? maxBatchSize : remainingItems;
                    var command = dbConnection.CreateCommand();
                    for (var j = 0; j < iterationLength; j++) {
                        sql += $"(NEWID(), @QueueName{j}, @Payload{j}, GETUTCDATE(), 0, 0)";
                        var isLastItem = j == iterationLength - 1;
                        sql += !isLastItem ? ", " : ";";
                        command.AddParameterWithValue($"@QueueName{j}", _queueName, DbType.String);
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
            throw new NotImplementedException();
        }
    }
}

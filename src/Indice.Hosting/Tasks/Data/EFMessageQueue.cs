using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.EntityFrameworkCore
{
    /// <summary>
    /// An implementation of <see cref="IMessageQueue{T}"/> using Entity Framework Core.
    /// </summary>
    /// <typeparam name="T">The type of queue item.</typeparam>
    public class EFMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly TaskDbContext _dbContext;
        private readonly string _queueName;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="queueNameResolver"></param>
        /// <param name="workerJsonOptions"></param>
        public EFMessageQueue(TaskDbContext dbContext, IQueueNameResolver<T> queueNameResolver, WorkerJsonOptions workerJsonOptions) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _queueName = queueNameResolver?.Resolve() ?? throw new ArgumentNullException(nameof(queueNameResolver));
            _jsonSerializerOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        // https://thinkrethink.net/2017/10/02/implement-pessimistic-concurrency-in-entity-framework-core/
        // For pessimistic concurrency check the link above.
        /// <inheritdoc/>
        public async Task<QMessage<T>> Dequeue() {
            var successfullLock = false;
            DbQMessage message;
            do {
                message = await GetAvailableItems().FirstOrDefaultAsync();
                if (message == null) {
                    return default;
                }
                message.DequeueCount++;
                message.State = QMessageState.Dequeued;
                try {
                    await _dbContext.SaveChangesAsync();
                    successfullLock = true;
                } catch (DbUpdateException) {
                    // Could not aquire lock. Will try again.
                }
            } 
            while (!successfullLock);
            return message.ToModel<T>(_jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public async Task Enqueue(T item, Guid? messageId, bool isPoison) {
            if (!messageId.HasValue) {
                var message = new DbQMessage {
                    Id = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    State = QMessageState.New,
                    Payload = JsonSerializer.Serialize(item, _jsonSerializerOptions),
                    QueueName = _queueName
                };
                _dbContext.Add(message);
            } else {
                var message = await _dbContext.Queue.Where(x => x.Id == messageId.Value).SingleAsync();
                message.State = isPoison ? QMessageState.Poison : QMessageState.New;
                message.DequeueCount++;
                _dbContext.Update(message);
            }
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task EnqueueRange(IEnumerable<T> items) {
            _dbContext.AddRange(items.Select(x => new DbQMessage() {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                State = QMessageState.New,
                Payload = JsonSerializer.Serialize(x, _jsonSerializerOptions),
                QueueName = _queueName
            }));
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<T> Peek() {
            var message = await GetAvailableItems().SingleOrDefaultAsync();
            return JsonSerializer.Deserialize<T>(message.Payload, _jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public async Task<int> Count() => await GetAvailableItems().CountAsync();

        /// <inheritdoc/>
        public async Task Cleanup(int? batchSize = null) {
            var query = @"
                DELETE FROM [work].[QMessage] 
                WHERE Id IN (SELECT TOP ({0}) Id FROM [work].[QMessage] 
                WHERE [State] = {1} AND [QueueName] = {2}
                ORDER BY Date);
            ";
            await _dbContext.Database.ExecuteSqlRawAsync(query, batchSize ?? 1000, QMessageState.Dequeued, _queueName);
        }

        private IQueryable<DbQMessage> GetAvailableItems() => _dbContext.Queue.Where(x => x.QueueName == _queueName && x.State == QMessageState.New).OrderBy(x => x.Date);
    }
}

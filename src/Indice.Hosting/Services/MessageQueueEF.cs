using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Services
{
    /// <summary>
    /// An implementation of <see cref="IMessageQueue{T}"/> using Entity Framework Core.
    /// </summary>
    /// <typeparam name="T">The type of queue item.</typeparam>
    [Obsolete("This implementation is fully functional but not very efficient performance wise.")]
    public class MessageQueueEF<T> : IMessageQueue<T> where T : class
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
        public MessageQueueEF(TaskDbContext dbContext, IQueueNameResolver<T> queueNameResolver, WorkerJsonOptions workerJsonOptions) {
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
                    // Could not acquire lock. Will try again.
                }
            }
            while (!successfullLock);
            return message.ToModel<T>(_jsonSerializerOptions);
        }

        /// <inheritdoc/>
        public async Task Enqueue(QMessage<T> item, bool isPoison) {
            DbQMessage message;
            var messageId = Guid.Parse(item.Id);
            if (item.IsNew) {
                message = new DbQMessage {
                    Id = messageId,
                    Date = item.Date,
                    State = QMessageState.New,
                    Payload = JsonSerializer.Serialize(item.Value, _jsonSerializerOptions),
                    QueueName = _queueName
                };
                _dbContext.Add(message);
            } else {
                message = await _dbContext.Queue.Where(x => x.Id == messageId).SingleAsync();
                message.State = isPoison ? QMessageState.Poison : QMessageState.New;
                message.DequeueCount = item.DequeueCount;
                _dbContext.Update(message);
            }
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task EnqueueRange(IEnumerable<QMessage<T>> items) {
            _dbContext.AddRange(items.Select(item => new DbQMessage() {
                Id = Guid.Parse(item.Id),
                Date = item.Date,
                State = QMessageState.New,
                Payload = JsonSerializer.Serialize(item.Value, _jsonSerializerOptions),
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
            var itemsToDelete = _dbContext.Queue
                .Where(x => x.QueueName == _queueName && x.State == QMessageState.Dequeued)
                .OrderBy(x => x.Date)
                .Take(batchSize ?? 1000)
                .ToListAsync();
            _dbContext.RemoveRange(itemsToDelete);
            await _dbContext.SaveChangesAsync();
        }

        private IQueryable<DbQMessage> GetAvailableItems() => _dbContext.Queue.Where(x => x.QueueName == _queueName && x.State == QMessageState.New).OrderBy(x => x.Date);
    }
}

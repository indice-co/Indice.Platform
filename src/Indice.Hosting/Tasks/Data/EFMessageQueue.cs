using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// EF message quueue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EFMessageQueue<T> : IMessageQueue<T> where T : class
    {
        private readonly TaskDbContext _DbContext;
        private readonly string _QueueName;
        private readonly JsonSerializerOptions _JsonOptions;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="nameResolver"></param>
        /// <param name="workerJsonOptions"></param>
        public EFMessageQueue(TaskDbContext dbContext, IQueueNameResolver<T> nameResolver, WorkerJsonOptions workerJsonOptions) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _QueueName = nameResolver?.Resolve() ?? throw new ArgumentNullException(nameof(nameResolver));
            _JsonOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /// <inheritdoc/>
        public async Task<QMessage<T>> Dequeue() {
            var successfullLock = false;
            DbQMessage message;
            do {
                message = await GetAvailableItems().FirstOrDefaultAsync();
                if (message == null)
                    return default;
                message.DequeueCount++;
                message.State = QMessageState.Dequeued;
                try {
                    await _DbContext.SaveChangesAsync();
                    successfullLock = true;
                } catch (DbUpdateException) {
                    // Could not aquire lock. Will try again.
                }
            } while (!successfullLock);
            return message.ToModel<T>(_JsonOptions);
        }

        /// <inheritdoc/>
        public async Task Enqueue(T item, Guid? messageId, bool isPoison) {
            if (!messageId.HasValue) {
                var message = new DbQMessage() {
                    Id = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    State = QMessageState.New,
                    Payload = JsonSerializer.Serialize(item, _JsonOptions),
                    QueueName = _QueueName
                };
                _DbContext.Add(message);
            } else {
                var message = await _DbContext.Queue.Where(x => x.Id == messageId.Value).SingleAsync();
                message.State = isPoison ? QMessageState.Poison : QMessageState.New;
                message.DequeueCount++;
                _DbContext.Update(message);
            }
            await _DbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task EnqueueRange(IEnumerable<T> items) {
            _DbContext.AddRange(items.Select(x => new DbQMessage() {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                State = QMessageState.New,
                Payload = JsonSerializer.Serialize(x, _JsonOptions),
                QueueName = _QueueName
            }));
            await _DbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<T> Peek() {
            var message = await GetAvailableItems().SingleOrDefaultAsync();
            return JsonSerializer.Deserialize<T>(message.Payload, _JsonOptions);
        }

        /// <inheritdoc/>
        public async Task<int> Count() {
            return await GetAvailableItems().CountAsync();
        }

        /// <inheritdoc/>
        public async Task Cleanup(int? batchSize = null) {
            //var items = await _DbContext.Queue
            //    .AsNoTracking()
            //    .Where(x => /*x.Date <= DateTime.Now.AddDays(-7) && */x.State == QMessageState.Dequeued)
            //    .OrderBy(x => x.Date)
            //    .Take(batchSize ?? 1000)
            //    //.Select(x => new DbQMessage { Id = x.Id, RowVersion = x.RowVersion })
            //    .ToListAsync();
            //_DbContext.RemoveRange(items);
            //await _DbContext.SaveChangesAsync();
            var query = @"
                DELETE FROM [work].[QMessage] 
                WHERE Id IN (SELECT TOP ({0}) Id FROM [work].[QMessage] 
                WHERE [State] = {1} AND [QueueName] = {2}
                ORDER BY Date);
            ";
            await _DbContext.Database.ExecuteSqlRawAsync(query, batchSize ?? 1000, QMessageState.Dequeued, _QueueName);
        }

        private IQueryable<DbQMessage> GetAvailableItems() => _DbContext.Queue.Where(x => x.QueueName == _QueueName && x.State == QMessageState.New).OrderBy(x => x.Date);
    }
}

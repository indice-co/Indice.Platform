using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math.EC.Rfc7748;

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

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="nameResolver"></param>
        public EFMessageQueue(TaskDbContext dbContext, IQueueNameResolver<T> nameResolver) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _QueueName = nameResolver?.Resolve() ?? throw new ArgumentNullException(nameof(nameResolver));
        }

        /// <inheritdoc/>
        public async Task<QMessage<T>> Dequeue() {
            bool successfullLock = false;
            DbQMessage message;
            do {
                message = await GetAvailableItems().FirstOrDefaultAsync();
                if (message == null)
                    return default(QMessage<T>);
                message.DequeueCount++;
                message.State = QMessageState.Dequeued;
                try {
                    await _DbContext.SaveChangesAsync();
                    successfullLock = true;
                } catch (DbUpdateException) {
                    // could not aquire lock. will try again.
                }
            } while (!successfullLock);
            return message.ToModel<T>();
        }

        /// <inheritdoc/>
        public async Task Enqueue(T item, Guid? messageId, bool isPoison) {
            if (!messageId.HasValue) {
                var message = new DbQMessage() {
                    Id = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    State = QMessageState.New,
                    Payload = JsonSerializer.Serialize(item),
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
                Payload = JsonSerializer.Serialize(x),
                QueueName = _QueueName
            }));
            await _DbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<T> Peek() {
            var message = await GetAvailableItems().SingleOrDefaultAsync();
            return JsonSerializer.Deserialize<T>(message.Payload);
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
            await _DbContext.Database.ExecuteSqlRawAsync(
@"DELETE FROM [work].[QMessage] 
WHERE Id IN (SELECT TOP ({0}) Id FROM [work].[QMessage] 
             WHERE [State] = {1}
             ORDER BY Date)", batchSize ?? 1000, QMessageState.Dequeued);
        }

        private IQueryable<DbQMessage> GetAvailableItems() {
            return _DbContext.Queue.Where(x => x.QueueName == _QueueName && x.State == QMessageState.New).OrderBy(x => x.Date);
        }
    }
}

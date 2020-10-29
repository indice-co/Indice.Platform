using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly TimeSpan _leaseDuration = TimeSpan.FromSeconds(30);

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
        public async Task<int> Count() {
            return await GetAvailableItems().CountAsync();
        }

        /// <inheritdoc/>
        public async Task<T> Dequeue() {
            bool successfullLock = false;
            DbQMessage message;
            do {
                message = await GetAvailableItems().FirstOrDefaultAsync();
                if (message == null)
                    return default(T);
                message.DequeueCount++;
                message.Status = QMessageStatus.Dequeued;
                try {
                    await _DbContext.SaveChangesAsync();
                    successfullLock = true;
                } catch (DbUpdateException) {
                    // could not aquire lock. will try again.
                }
            } while (!successfullLock);
            return JsonSerializer.Deserialize<T>(message.Payload);
        }
        /// <inheritdoc/>
        public async Task Enqueue(T item) {
            var message = new DbQMessage() {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Status = QMessageStatus.New,
                Payload = JsonSerializer.Serialize(item),
                QueueName = _QueueName
            };
            _DbContext.Add(message);
            await _DbContext.SaveChangesAsync();
        }
        /// <inheritdoc/>
        public async Task<T> Peek() {
            var message = await GetAvailableItems().SingleOrDefaultAsync();
            return JsonSerializer.Deserialize<T>(message.Payload);
        }

        private IQueryable<DbQMessage> GetAvailableItems() {
            return _DbContext.Queue.Where(x => x.QueueName == _QueueName && x.Status == QMessageStatus.New).OrderBy(x => x.Date);
        }
    }
}

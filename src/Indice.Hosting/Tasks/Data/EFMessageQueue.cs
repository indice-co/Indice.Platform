using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        public async Task<T> Peek() {
            var message = await GetAvailableItems().SingleOrDefaultAsync();
            return JsonSerializer.Deserialize<T>(message.Payload);
        }

        private IQueryable<DbQMessage> GetAvailableItems() {
            return _DbContext.Queue.Where(x => x.QueueName == _QueueName && x.State == QMessageState.New).OrderBy(x => x.Date);
        }
    }
}

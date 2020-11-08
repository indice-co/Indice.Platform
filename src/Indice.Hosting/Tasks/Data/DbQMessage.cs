using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// A queue message
    /// </summary>
    public class DbQMessage
    {
        /// <summary>
        /// The id
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The Queue name
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// The payload
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// The row version.
        /// </summary>
        public byte[] RowVersion { get; set; }
        /// <summary>
        /// The dequeue count.
        /// </summary>
        public int DequeueCount { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public QMessageState State { get; set; }

        /// <summary>
        /// Generate the dto for this <see cref="DbQMessage"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public QMessage<T> ToModel<T>(JsonSerializerOptions options = null) where T : class {
            return new QMessage<T> {
                Id = Id,
                Date = Date,
                DequeueCount = DequeueCount,
                QueueName = QueueName,
                Value = JsonSerializer.Deserialize<T>(Payload, options ?? WorkerJsonOptions.GetDefaultSettings())
            };
        }
    }
}

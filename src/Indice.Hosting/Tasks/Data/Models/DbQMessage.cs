using System;
using System.Text.Json;

namespace Indice.Hosting.Data
{
    /// <summary>
    /// A queue message.
    /// </summary>
    public class DbQMessage
    {
        /// <summary>
        /// The id.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The queue name.
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// The payload.
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
        /// Generate the DTO for this <see cref="DbQMessage"/>.
        /// </summary>
        /// <typeparam name="T">The type of message to convert to.</typeparam>
        public QMessage<T> ToModel<T>(JsonSerializerOptions options = null) where T : class => new QMessage<T> {
            Id = Id,
            Date = Date,
            DequeueCount = DequeueCount,
            QueueName = QueueName,
            Value = JsonSerializer.Deserialize<T>(Payload, options ?? WorkerJsonOptions.GetDefaultSettings())
        };
    }
}

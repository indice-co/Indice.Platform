using System;
using System.Collections.Generic;
using System.Text;

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
        /// The date.
        /// </summary>
        public byte[] RowVersion { get; set; }
        /// <summary>
        /// The dequeue count.
        /// </summary>
        public int DequeueCount { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public QMessageStatus Status { get; set; }
    }
}

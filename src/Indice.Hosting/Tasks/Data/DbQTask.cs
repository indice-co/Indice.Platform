using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Tracks a queue message task
    /// </summary>
    public class DbQTask
    {
        /// <summary>
        /// The id
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Alias (alternate key)
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Task group
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// Task name
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type name
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime DateStarted { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime DateCompleted { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime ExecutionCount { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public QTaskStatus Status { get; set; }
        /// <summary>
        /// The errors
        /// </summary>
        public string Errors { get; set; }
        /// <summary>
        /// The payload
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public double Progress { get; set; }
    }
}

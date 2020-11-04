using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// The default implementation for the <see cref="QTask{TState}"/> with state as <see cref="Dictionary{String, Object}"/>
    /// </summary>
    public class QTaskDefault : QTask<Dictionary<string, object>>
    { 
    
    }

    /// <summary>
    /// A dto representing a worker task 
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class QTask<TState> where TState : class
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
        public TState State { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public double Progress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// The queue message status
    /// </summary>
    public enum QMessageStatus
    {
        /// <summary>
        /// Not dequeued (processed)
        /// </summary>
        New = 0,
        /// <summary>
        /// Dequeued (removed)
        /// </summary>
        Dequeued = 1
    }

    /// <summary>
    /// Task status. As in Job state persisted state (in quarz lingo)
    /// </summary>
    public enum QTaskStatus
    {
        /// <summary>
        /// Task waiting to be executed
        /// </summary>
        New = 0,
        /// <summary>
        /// Running task
        /// </summary>
        Running = 1,
        /// <summary>
        /// Completed task
        /// </summary>
        Completed= 1
    }
}

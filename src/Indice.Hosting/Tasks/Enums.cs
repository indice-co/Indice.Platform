namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// The queue message status
    /// </summary>
    public enum QMessageState
    {
        /// <summary>
        /// Marks a poison message.
        /// </summary>
        Poison = -1,
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
    public enum ScheduledTaskStatus
    {
        /// <summary>
        /// Task waiting to be executed
        /// </summary>
        Idle = 0,
        /// <summary>
        /// Running task
        /// </summary>
        Running = 1,
    }
}

namespace Indice.Hosting
{
    /// <summary>
    /// Describes the current status of a <see cref="WorkItem"/>.
    /// </summary>
    public enum WorkItemStatus
    {
        /// <summary>
        /// An item that waits to be processed.
        /// </summary>
        Pending = 0,
        /// <summary>
        /// An item that has been processed successfully.
        /// </summary>
        Completed = 1,
        /// <summary>
        /// An item that failed to be processed.
        /// </summary>
        Failed = 2
    }
}

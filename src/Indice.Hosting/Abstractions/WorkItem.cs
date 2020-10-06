using System;
using Indice.Types;

namespace Indice.Hosting
{
    /// <summary>
    /// The base class that a work item needs to inherit.
    /// </summary>
    public abstract class WorkItem
    {
        /// <summary>
        /// The <see cref="DateTime"/> that indicates the end of lease.
        /// </summary>
        public DateTime? LeaseEndDate { get; set; }
        /// <summary>
        /// The id that a work item owns when it is processed.
        /// </summary>
        public string LeaseId { get; } = new Base64Id(Guid.NewGuid());
        /// <summary>
        /// The status of the work item.
        /// </summary>
        public WorkItemStatus Status { get; set; }
    }

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

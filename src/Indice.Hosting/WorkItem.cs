using System;
using Indice.Types;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class WorkItem<T> where T : class
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTime? LeaseEndDate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LeaseId { get; } = new Base64Id(Guid.NewGuid());
        /// <summary>
        /// 
        /// </summary>
        public WorkItemStatus Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public T Data { get; set; }
    }
}

using System;

namespace Indice.Hosting.Data
{
    /// <summary>
    /// Tracks a queue message task.
    /// </summary>
    public class DbLock
    {
        /// <summary>
        /// The id.
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// The subject of the lock.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Lock lease expiration date.
        /// </summary>
        public DateTime ExpirationDate { get; set; }
        /// <summary>
        /// Lock lease duration. Used in order to renew an existing lease.
        /// </summary>
        public int Duration { get; set; } = 30;
    }
}

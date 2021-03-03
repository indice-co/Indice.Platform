using System;

namespace Indice.Hosting.EntityFrameworkCore
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
    }
}

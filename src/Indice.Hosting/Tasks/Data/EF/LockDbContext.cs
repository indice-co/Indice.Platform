using Indice.Hosting.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.EntityFrameworkCore
{
    /// <summary>
    /// Only use with caution in <see cref="SqlServerLockManager"/>.
    /// </summary>
    public class LockDbContext : TaskDbContext
    {
        /// <summary>
        /// Constructs a new <see cref="LockDbContext"/>.
        /// </summary>
        /// <param name="options"></param>
        public LockDbContext(DbContextOptions<LockDbContext> options) : base(options) { }
    }
}

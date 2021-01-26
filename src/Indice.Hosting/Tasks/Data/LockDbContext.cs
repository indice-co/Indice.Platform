using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Only use with caution in <see cref="EFLockManager"/>.
    /// </summary>
    public class LockDbContext : TaskDbContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public LockDbContext(DbContextOptions<LockDbContext> options) : base(options) { }
    }
}

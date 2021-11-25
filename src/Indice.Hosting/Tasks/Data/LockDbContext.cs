
/* Unmerged change from project 'Indice.Hosting (net5.0)'
Before:
using Microsoft.EntityFrameworkCore;
After:
using Indice;
using Indice.Hosting;
using Indice.Hosting.Tasks;
using Indice.Hosting.Tasks.Data;
using Indice.Hosting.Tasks.Data;
using Indice.Hosting.Tasks.Data.EF;
using Microsoft.EntityFrameworkCore;
*/
using Indice.Hosting.Tasks.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Only use with caution in <see cref="RelationalLockManager"/>.
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

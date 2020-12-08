using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Only use with caution in <see cref="EFLockManager"/>
    /// </summary>
    public class LockDbContext : TaskDbContext
    {
        public LockDbContext(DbContextOptions<LockDbContext> options) : base(options) {
        }
    }
}

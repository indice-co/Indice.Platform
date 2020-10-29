using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// A db context for hosting multiple <see cref="IMessageQueue{T}"/> 
    /// </summary>
    public class TaskDbContext : DbContext
    {
        /// <summary>
        /// create the DbContext 
        /// </summary>
        /// <param name="options"></param>
        public TaskDbContext(DbContextOptions options) : base(options) {
#if DEBUG
            Database.EnsureCreated();
#endif
        }

        /// <summary>
        /// Queue messages
        /// </summary>
        public DbSet<DbQMessage> Queue { get; set; }
        /// <summary>
        /// Tasks
        /// </summary>
        public DbSet<DbQTask> Tasks { get; set; }

        /// <summary>
        /// Configure the context
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new DbQMessageMap());
            builder.ApplyConfiguration(new DbQTaskMap());
        }
    }
}

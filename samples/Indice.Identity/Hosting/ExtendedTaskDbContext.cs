using Indice.Hosting.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Indice.Identity.Hosting
{
    public class ExtendedTaskDbContext : TaskDbContext
    {
        public ExtendedTaskDbContext(DbContextOptions<ExtendedTaskDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
        }
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Indice.Identity.Data
{
    public class IndiceDbContext : DbContext
    {
        public IndiceDbContext(DbContextOptions options) : base(options) {
#if DEBUG
            Database.EnsureCreated();
#endif
        }

        public DbSet<DbUserMessage> UserMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}

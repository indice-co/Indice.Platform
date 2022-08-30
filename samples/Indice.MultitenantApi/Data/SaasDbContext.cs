using System.Reflection;
using Indice.MultitenantApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.MultitenantApi.Data
{
    public class SaasDbContext : DbContext
    {
        public SaasDbContext(DbContextOptions<SaasDbContext> options) : base(options) {
#if DEBUG
            if (Database.EnsureCreated()) {
                this.Seed();
            }
#endif
        }

        public DbSet<DbSubscription> Subscriptions { get; set; }
        public DbSet<DbSubscriptionMember> SubscriptionMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetAssembly(typeof(SaasDbContext)));
        }
    }
}

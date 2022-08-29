using Indice.MultitenantApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.MultitenantApi.Data
{
    public class SaasDbContext : DbContext
    {
        public DbSet<DbSubscription> Subscriptions { get; set; }
        public DbSet<DbSubscriptionMember> SubscriptionMembers { get; set; }
    }
}

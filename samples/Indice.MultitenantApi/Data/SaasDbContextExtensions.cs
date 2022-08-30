using Indice.MultitenantApi.Data.Models;
using Indice.MultitenantApi.Models;

namespace Indice.MultitenantApi.Data
{
    public static class SaasDbContextExtensions
    {
        public static void Seed(this SaasDbContext dbContext) {
            var subscription = new DbSubscription { 
                Alias = "contoso-ltd",
                DatabaseConnection = "Server=(localdb)\\MSSQLLocalDB;Database=Contoso.ApiDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                Id = Guid.NewGuid(),
                Status = SubscriptionStatus.Enabled
            };
            dbContext.Subscriptions.Add(subscription);
            dbContext.SubscriptionMembers.Add(new DbSubscriptionMember {
                AccessLevel = MemberAccessLevel.Owner,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = "g.manoltzas@indice.gr",
                Id = Guid.NewGuid(),
                MemberId = Guid.Parse("4bbd83b8-fa87-4b83-9baf-e7d02591d5be"),
                Status = MembershipStatus.Active,
                Subscription = subscription,
                SubscriptionId = subscription.Id
            });
            dbContext.SaveChanges();
        }
    }
}

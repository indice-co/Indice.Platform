using Indice.Sample.Common.Data.Models;
using Indice.Sample.Common.Models;

namespace Indice.Sample.Common.Data;

public static class SaasDbContextExtensions
{
    public static void Seed(this SaasDbContext dbContext) {
        var subscription1 = new DbSubscription {
            Alias = "contoso-ltd",
            DatabaseConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Contoso.Messages;Trusted_Connection=True;MultipleActiveResultSets=true",
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Enabled
        };
        var subscription2 = new DbSubscription {
            Alias = "acme-corp",
            DatabaseConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Acme.Messages;Trusted_Connection=True;MultipleActiveResultSets=true",
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Enabled
        };
        var subscription3 = new DbSubscription {
            Alias = "wayne-enterprises",
            DatabaseConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Wayne.Messages;Trusted_Connection=True;MultipleActiveResultSets=true",
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Enabled
        };
        var subscription4 = new DbSubscription { 
            Alias = "stark-industries",
            DatabaseConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Stark.Messages;Trusted_Connection=True;MultipleActiveResultSets=true",
            Id = Guid.NewGuid(),
            Status = SubscriptionStatus.Enabled
        };
        dbContext.Subscriptions.AddRange(subscription1, subscription2, subscription3, subscription4);
        dbContext.SubscriptionMembers.AddRange(
            new DbSubscriptionMember {
                AccessLevel = MemberAccessLevel.Owner,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = "g.manoltzas@indice.gr",
                Id = Guid.NewGuid(),
                MemberId = Guid.Parse("4bbd83b8-fa87-4b83-9baf-e7d02591d5be"),
                Status = MembershipStatus.Active,
                Subscription = subscription1,
                SubscriptionId = subscription1.Id
            },
            new DbSubscriptionMember {
                AccessLevel = MemberAccessLevel.Owner,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = "g.manoltzas@indice.gr",
                Id = Guid.NewGuid(),
                MemberId = Guid.Parse("4bbd83b8-fa87-4b83-9baf-e7d02591d5be"),
                Status = MembershipStatus.Active,
                Subscription = subscription2,
                SubscriptionId = subscription2.Id
            },
            new DbSubscriptionMember {
                AccessLevel = MemberAccessLevel.Member,
                CreatedAt = DateTimeOffset.UtcNow,
                Email = "g.manoltzas@indice.gr",
                Id = Guid.NewGuid(),
                MemberId = Guid.Parse("4bbd83b8-fa87-4b83-9baf-e7d02591d5be"),
                Status = MembershipStatus.Active,
                Subscription = subscription3,
                SubscriptionId = subscription3.Id
            });
        dbContext.SaveChanges();
    }
}

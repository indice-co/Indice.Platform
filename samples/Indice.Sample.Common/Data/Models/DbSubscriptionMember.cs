using Indice.Sample.Common.Models;

namespace Indice.Sample.Common.Data.Models;

public class DbSubscriptionMember
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public Guid SubscriptionId { get; set; }
    public MembershipStatus Status { get; set; }
    public string Email { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastAccessDate { get; set; }
    public MemberAccessLevel AccessLevel { get; set; }
    public virtual DbSubscription Subscription { get; set; }
}

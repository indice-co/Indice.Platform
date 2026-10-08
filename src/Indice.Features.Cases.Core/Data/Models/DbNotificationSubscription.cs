using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbNotificationSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseTypeId { get; set; }
    public Subscriber Subscriber { get; set; } = new();
}
#pragma warning restore 1591


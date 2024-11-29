namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbNotificationSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseTypeId { get; set; }
    public string Email { get; set; }
    public string GroupId { get; set; }
}
#pragma warning restore 1591


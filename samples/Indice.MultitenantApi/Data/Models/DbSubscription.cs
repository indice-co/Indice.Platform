using Indice.MultitenantApi.Models;
using Indice.Security;

namespace Indice.MultitenantApi.Data.Models
{
    public class DbSubscription : ITenantWithAlias
    {
        public Guid Id { get; set; }
        public string Alias { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Enabled;
        public string DatabaseConnectionString { get; set; }
        public string PushNotificationsConnectionString { get; set; }
    }
}

using Indice.Features.Multitenancy.Core;

namespace Indice.MultitenantApi.Models
{
    public class ExtendedTenant : Tenant
    {
        public string PushNotificationConnectionString { get; set; }
    }
}

using Indice.Features.Multitenancy.AspNetCore;

namespace Indice.MultitenantApi.Models
{
    public class ExtendedTenant : Tenant
    {
        public string PushNotificationConnectionString { get; set; }
    }
}

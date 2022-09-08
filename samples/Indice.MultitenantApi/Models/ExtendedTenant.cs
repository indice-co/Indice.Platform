using Indice.AspNetCore.MultiTenancy;

namespace Indice.MultitenantApi.Models
{
    public class ExtendedTenant : Tenant
    {
        public string PushNotificationConnectionString { get; set; }
    }
}

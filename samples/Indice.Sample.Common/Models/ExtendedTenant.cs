using Indice.Features.Multitenancy.Core;

namespace Indice.Sample.Common.Models
{
    public class ExtendedTenant : Tenant
    {
        public string PushNotificationConnectionString { get; set; }
    }
}

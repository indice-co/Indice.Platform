using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal interface ITrustedDeviceRegistrationRequestValidator
    {
        Task<TrustedDeviceRegistrationRequestValidationResult> ValidateAsync(NameValueCollection parameters);
    }
}

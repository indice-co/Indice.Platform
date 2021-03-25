using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal interface ITrustedDeviceRegistrationRequestValidator
    {
        Task<TrustedDeviceRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters);
    }
}

using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationResponseGenerator : ITrustedDeviceRegistrationResponseGenerator
    {
        public Task<TrustedDeviceRegistrationResponse> ProcessAsync(TrustedDeviceRegistrationRequestValidationResult validationResult) {
            throw new System.NotImplementedException();
        }
    }
}

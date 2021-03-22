using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal interface ITrustedDeviceRegistrationResponseGenerator
    {
        Task<TrustedDeviceRegistrationResponse> ProcessAsync(TrustedDeviceRegistrationRequestValidationResult validationResult);
    }
}

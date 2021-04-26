using System;
using System.Threading.Tasks;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class TrustedDeviceExtensionGrantValidator : IExtensionGrantValidator
    {
        public string GrantType => "trusted_device";

        public Task ValidateAsync(ExtensionGrantValidationContext context) {
            throw new NotImplementedException();
        }
    }
}

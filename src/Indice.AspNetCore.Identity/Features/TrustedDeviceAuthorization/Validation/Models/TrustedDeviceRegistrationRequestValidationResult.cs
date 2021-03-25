using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationRequestValidationResult : ValidationResult
    {
        public ClaimsPrincipal Principal { get; set; }
        public Client Client { get; set; }
        public IList<string> RequestedScopes { get; set; }
    }
}

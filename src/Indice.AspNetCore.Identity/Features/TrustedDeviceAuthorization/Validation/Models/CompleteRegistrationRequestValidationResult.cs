using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Types;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class CompleteRegistrationRequestValidationResult : ValidationResult
    {
        public ClaimsPrincipal Principal { get; set; }
        public Client Client { get; set; }
        public DevicePlatform DevicePlatform { get; set; }
        public IList<string> RequestedScopes { get; set; }
        public InteractionMode InteractionMode { get; set; }
        public UserDevice Device { get; private set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Pin { get; set; }
        public string PublicKey { get; set; }
        public string UserId { get; set; }

        public void SetDevice(UserDevice device) => Device = device;
    }
}

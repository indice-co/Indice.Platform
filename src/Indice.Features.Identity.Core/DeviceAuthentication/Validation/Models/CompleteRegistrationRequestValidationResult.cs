using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Validation
{
    internal class CompleteRegistrationRequestValidationResult : ValidationResult
    {
        public ClaimsPrincipal Principal { get; set; }
        public Client Client { get; set; }
        public DevicePlatform DevicePlatform { get; set; }
        public IList<string> RequestedScopes { get; set; }
        public InteractionMode InteractionMode { get; set; }
        public UserDevice Device { get; set; }
        public User User { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Pin { get; set; }
        public string PublicKey { get; set; }
    }
}

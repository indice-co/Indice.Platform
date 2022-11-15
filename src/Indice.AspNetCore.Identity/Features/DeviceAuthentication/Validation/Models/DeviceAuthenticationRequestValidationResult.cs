using System.Collections.Generic;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Validation
{
    internal class DeviceAuthenticationRequestValidationResult : ValidationResult
    {
        public bool IsOpenIdRequest { get; set; }
        public Client Client { get; set; }
        public IList<string> RequestedScopes { get; set; }
        public InteractionMode InteractionMode { get; set; }
        public string CodeChallenge { get; set; }
        public string UserId { get; set; }
        public UserDevice Device { get; set; }
    }
}

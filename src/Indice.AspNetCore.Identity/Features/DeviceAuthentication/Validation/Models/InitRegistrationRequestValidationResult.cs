using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;

namespace Indice.AspNetCore.Identity.DeviceAuthentication.Validation
{
    internal class InitRegistrationRequestValidationResult : ValidationResult
    {
        public ClaimsPrincipal Principal { get; set; }
        public Client Client { get; set; }
        public IList<string> RequestedScopes { get; set; }
        public InteractionMode InteractionMode { get; set; }
        public string CodeChallenge { get; set; }
        public string DeviceId { get; set; }
        public string UserId { get; set; }
        public TotpDeliveryChannel DeliveryChannel { get; set; }
    }
}

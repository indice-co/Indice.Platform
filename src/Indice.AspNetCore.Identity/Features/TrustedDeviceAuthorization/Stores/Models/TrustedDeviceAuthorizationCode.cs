using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Indice.AspNetCore.Identity.Features
{
    public class TrustedDeviceAuthorizationCode
    {
        public DateTime CreationTime { get; set; }
        public int Lifetime { get; set; }
        public string ClientId { get; set; }
        public ClaimsPrincipal Subject { get; set; }
        public IEnumerable<string> RequestedScopes { get; set; }
        public string Description { get; set; }
    }
}

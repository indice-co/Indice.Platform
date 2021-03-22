using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// 
    /// </summary>
    public class TrustedDeviceAuthorizationCode
    {
        /// <summary>
        /// 
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Lifetime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ClaimsPrincipal Subject { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<string> RequestedScopes { get; set; }
    }
}

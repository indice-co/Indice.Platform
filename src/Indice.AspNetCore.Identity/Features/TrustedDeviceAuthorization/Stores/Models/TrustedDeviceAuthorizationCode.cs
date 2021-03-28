﻿using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models an authorization code for a trusted device.
    /// </summary>
    public class TrustedDeviceAuthorizationCode
    {
        /// <summary>
        /// The <see cref="DateTime"/> that the code was created.
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// The lifetime of the code expressed in seconds.
        /// </summary>
        public int Lifetime { get; set; }
        /// <summary>
        /// The client id.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The principal associated with this code.
        /// </summary>
        public ClaimsPrincipal Subject { get; set; }
        /// <summary>
        /// The scopes that were requested.
        /// </summary>
        public IEnumerable<string> RequestedScopes { get; set; }
        /// <summary>
        /// The description.
        /// </summary>
        public string Description { get; set; }
    }
}

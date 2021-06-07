// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Indice.AspNetCore.Authentication.Apple
{
    /// <summary>
    /// Configuration options for apple open id connect 
    /// </summary>
    public class AppleOptions
    {
        /// <summary>
        /// Essentially this is the client id
        /// </summary>
        public string ServiceId { get; set; }
        /// <summary>
        /// The contents of the P8 key as base64 string.
        /// </summary>
        public string ServicePrivateKey { get; set; }
        /// <summary>
        /// your accounts team ID found in the dev portal
        /// </summary>
        public string TeamId { get; set; }
        /// <summary>
        /// The request path within the application's base path where the user-agent will
        /// be returned. The middleware will process this request when it arrives.
        /// </summary>
        public PathString CallbackPath { get; set; }
        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware responsible
        /// of persisting user's identity after a successful authentication. This value typically
        /// corresponds to a cookie middleware registered in the Startup class. When omitted,
        /// Microsoft.AspNetCore.Authentication.AuthenticationOptions.DefaultSignInScheme
        /// is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }
    }
}

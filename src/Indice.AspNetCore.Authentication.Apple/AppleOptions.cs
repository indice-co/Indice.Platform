// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Authentication.Apple
{
    /// <summary>
    /// Configuration options for Apple OpenID Connect.
    /// </summary>
    public class AppleOptions
    {
        /// <summary>
        /// The full app id
        /// </summary>
        public string AppId {
            get => !string.IsNullOrEmpty(ServiceId) ? $"{TeamId}.{ServiceId}" : null;
            set {
                ServiceId = !string.IsNullOrEmpty(value) ? value[(value.IndexOf('.') + 1)..] : null;
                TeamId = !string.IsNullOrEmpty(value) ? value[..(value.Length - ServiceId.Length - 1)] : null;
            }
        }
        /// <summary>
        /// The private key. The contents of the P8 key.
        /// </summary>
        public string PrivateKey { get; set; }
        /// <summary>
        /// Private key id.
        /// </summary>
        public string PrivateKeyId { get; set; }
        /// <summary>
        /// The client id. This should be a <see cref="ServiceId"/> created spesificly for web authentication.
        /// </summary>
        /// <remarks>
        /// Developers go in the identifiers section https://developer.apple.com/account/resources/identifiers
        /// </remarks>
        public string ServiceId { get; set; }
        /// <summary>
        /// Your account's team ID or AppId prefix.
        /// </summary>
        public string TeamId { get; set; }
        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned. The middleware will process this request when it arrives.
        /// </summary>
        public PathString CallbackPath { get; set; }
        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware responsible of persisting user's identity after a successful authentication. This value typically
        /// corresponds to a cookie middleware registered in the Startup class. When omitted, <see cref="AuthenticationOptions.DefaultSignInScheme"/> is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }
    }
}

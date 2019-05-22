// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Model used in login screen.
    /// </summary>
    public class LoginInputModel
    {
        /// <summary>
        /// The name of the user.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// User's password.
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Determines whether the login will be remembered after user closes browser window.
        /// </summary>
        public bool RememberLogin { get; set; }
        /// <summary>
        /// The URL to return.
        /// </summary>
        public string ReturnUrl { get; set; }
    }
}

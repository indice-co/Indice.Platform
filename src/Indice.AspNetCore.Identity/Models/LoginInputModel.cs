// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Regust view model for the login page
    /// </summary>
    public class LoginInputModel
    {
        /// <summary>
        /// The user name.
        /// </summary>
        [Required]
        public string Username { get; set; }
        /// <summary>
        /// The password
        /// </summary>
        [Required]
        public string Password { get; set; }
        /// <summary>
        /// Flag that indicates that the login cookie should be persisted
        /// </summary>
        public bool RememberLogin { get; set; }
        /// <summary>
        /// The return url after the login is successful.
        /// </summary>
        public string ReturnUrl { get; set; }
        /// <summary>
        /// The id of the current client in the request. 
        /// </summary>
        public string ClientId { get; set; }
    }
}

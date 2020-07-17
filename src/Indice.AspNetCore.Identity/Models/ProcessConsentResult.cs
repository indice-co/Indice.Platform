// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Represents a consent result object.
    /// </summary>
    public class ProcessConsentResult
    {
        /// <summary>
        /// The result is a redirection action
        /// </summary>
        public bool IsRedirect => RedirectUri != null;

        /// <summary>
        /// The redirect uri if available
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// The client
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// Should show the consent view.
        /// </summary>
        public bool ShowView => ViewModel != null;

        /// <summary>
        /// Consent view model
        /// </summary>
        public ConsentViewModel ViewModel { get; set; }

        /// <summary>
        /// checks to see if there is an error
        /// </summary>
        public bool HasValidationError => ValidationError != null;

        /// <summary>
        /// The validation error
        /// </summary>
        public string ValidationError { get; set; }
    }
}

// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Consent view model
    /// </summary>
    public class ConsentViewModel : ConsentInputModel
    {
        /// <summary>
        /// Client name
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// Client site
        /// </summary>
        public string ClientUrl { get; set; }
        /// <summary>
        /// Logo url of the client
        /// </summary>
        public string ClientLogoUrl { get; set; }

        /// <summary>
        /// Should the checkbox to allow remember be available
        /// </summary>
        public bool AllowRememberConsent { get; set; }

        /// <summary>
        /// Identity scopes
        /// </summary>
        public IEnumerable<ScopeViewModel> IdentityScopes { get; set; }

        /// <summary>
        /// Resource scopes
        /// </summary>
        public IEnumerable<ScopeViewModel> ResourceScopes { get; set; }

        /// <summary>
        /// Available Sca Methods
        /// </summary>
        public IEnumerable<ScaMethodViewModel> ScaMethods { get; set; }

        /// <summary>
        /// Strong customer authentication code sent show the input.
        /// </summary>
        public bool ShowScaCodeInput { get; set; }

        /// <summary>
        /// Requires Strong customer authentication
        /// </summary>
        public bool RequiresSca => ResourceScopes?.Where(x => x.RequiresSca).Any() == true;
    }
}

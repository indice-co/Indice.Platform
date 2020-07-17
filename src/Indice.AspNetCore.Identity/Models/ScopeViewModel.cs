// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Scope view model. for the consent screen
    /// </summary>
    public class ScopeViewModel
    {
        /// <summary>
        /// Scope name
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description. Could be plain text or markdown
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Should make the scope stand out
        /// </summary>
        public bool Emphasize { get; set; }

        /// <summary>
        /// Can deselect the scope on the consent screen or not
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Is preselected
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// Requires Strong customer authentication
        /// </summary>
        public bool RequiresSca { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// The consented (grants) view model
    /// </summary>
    public class GrantsViewModel
    {
        /// <summary>
        /// The given grants list
        /// </summary>
        public IEnumerable<GrantViewModel> Grants { get; set; }
    }

    /// <summary>
    /// View model for a given grant.
    /// </summary>
    public class GrantViewModel
    {
        /// <summary>
        /// the client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The name of the client
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// The url to the application site
        /// </summary>
        public string ClientUrl { get; set; }

        /// <summary>
        /// The logo image url
        /// </summary>
        public string ClientLogoUrl { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Date created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// If available the expiration date
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Any given identity grants (email, profile, address etc)
        /// </summary>
        public IEnumerable<string> IdentityGrantNames { get; set; }

        /// <summary>
        /// Access given to the client in order to access api resources
        /// </summary>
        public IEnumerable<string> ApiGrantNames { get; set; }
    }
}

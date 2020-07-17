using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// The request object
    /// </summary>
    public class ConsentInputModel
    {
        /// <summary>
        /// The button pressed
        /// </summary>
        public string Button { get; set; }

        /// <summary>
        /// Scopes consented
        /// </summary>
        public IEnumerable<string> ScopesConsented { get; set; }

        /// <summary>
        /// Remember selection
        /// </summary>
        public bool RememberConsent { get; set; }

        /// <summary>
        /// return url
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Selected Strong customer authentication method
        /// </summary>
        public string ScaMethod { get; set; }

        /// <summary>
        /// Strong customer authentication code.
        /// </summary>
        public string ScaCode { get; set; }
    }
}

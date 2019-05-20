using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// A view model for the available String Customer Authentication methods.
    /// </summary>
    public class ScaMethodViewModel
    {
        /// <summary>
        /// The name used to register the SCA method in the Users Token providers list
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// A boolean indicating the the sca method can generate TOTP codes or only validate them.
        /// </summary>
        public string CanGenerateTotp { get; set; }
    }
}

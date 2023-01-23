using System.Collections.Generic;
using System.Linq;

namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// This is a subclass of the <see cref="RegisterRequest"/> that also contains the external Identity providers that are setup.
    /// Only used on the MVC Views. 
    /// </summary>
    public class RegisterViewModel : RegisterRequest
    {
        /// <summary>
        /// List of external providers.
        /// </summary>
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; } = new List<ExternalProvider>();
        /// <summary>
        /// Visible external providers are those given a <see cref="ExternalProvider.DisplayName"/>
        /// </summary>
        public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        /// <summary>
        /// Optional flag that should hide the local user registration form and keep only the <see cref="ExternalProviders"/> options.
        /// </summary>
        public bool ExternalRegistrationOnly { get; set; }
        /// <summary>
        /// The authentication scheme of the external registration.
        /// </summary>
        public string ExternalRegistrationScheme => ExternalProviders?.SingleOrDefault()?.AuthenticationScheme;
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Indice.AspNetCore.Identity.Models
{
    public class RegisterViewModel : RegisterRequest
    {
        public IEnumerable<ExternalProvider> ExternalProviders { get; set; }
        public IEnumerable<ExternalProvider> VisibleExternalProviders => ExternalProviders.Where(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        public bool ExternalRegistrationOnly { get; set; }
        public string ExternalRegistrationScheme => ExternalProviders?.SingleOrDefault()?.AuthenticationScheme;
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Indice.Configuration
{
    /// <summary>
    /// Settings related to APIs.
    /// </summary>
    public class ApiSettings
    {
        /// <summary>
        /// Security wise the name of the API. Used for identifying the API as a resource in OAuth2 as well as the main scope.
        /// </summary>
        public string ResourceName { get; set; } = "api1";
        /// <summary>
        /// List of secrets. Usualy here is the API secret used to communicate with IdentityServer in order to exchange the reference tokens with actual info.
        /// </summary>
        public Dictionary<string, string> Secrets { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// The scopes available for the API.
        /// </summary>
        public List<Scope> Scopes { get; set; } = new List<Scope>();
        /// <summary>
        /// The scopes as dictionary.
        /// </summary>
        public Dictionary<string, string> ScopesDictionary {
            get => Scopes.ToDictionary(x => x.Name, x => x.Description);
            set => Scopes = value.Select(x => new Scope { Name = x.Key, Description = x.Value }).ToList();
        }
        /// <summary>
        /// Friendly name for the API.
        /// </summary>
        public string FriendlyName { get; set; } = "My API Name";
        /// <summary>
        /// The API verison number.
        /// </summary>
        public string DefaultVersion { get; set; } = "1";
        /// <summary>
        /// The API terms of service url.
        /// </summary>
        public string TermsOfServiceUrl { get; set; }
        /// <summary>
        /// The API licence url.
        /// </summary>
        public LegalDocument License { get; set; }
        /// <summary>
        /// API contact info (e.x. developer). Will appear usualy on the swagger documentation page.
        /// </summary>
        public ApiContact Contact { get; set; }

        /// <summary>
        /// Used to configure a legal document resource name and location
        /// </summary>
        public class LegalDocument
        {
            /// <summary>
            /// The name of the document.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The name of the document.
            /// </summary>
            public string Url { get; set; }
        }

        /// <summary>
        /// API contact info (e.x. developer). Will appear usualy on the swagger documentation page.
        /// </summary>
        public class ApiContact
        {
            /// <summary>
            /// The name of the contact.
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// A URL to the developer portal or GitHub account/repo.
            /// </summary>
            public string Url { get; set; }
            /// <summary>
            /// The contact email.
            /// </summary>
            public string Email { get; set; }
        }

        /// <summary>
        /// ApiSettings scope entry
        /// </summary>
        public class Scope
        {
            /// <summary>
            /// The scope value (ie identity.users)
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The scope descriptions
            /// </summary>
            public string Description { get; set; }
        }
    }
}

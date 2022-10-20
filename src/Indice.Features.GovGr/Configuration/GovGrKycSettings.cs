using System.Collections.Generic;

namespace Indice.Features.GovGr.Configuration
{
    /// <summary>
    /// The Kyc settings.
    /// </summary>
    public class GovGrKycSettings
    {
        /// <summary>
        /// The name of the AppSetting
        /// </summary>
        public const string Name = nameof(GovGrKycSettings);

        /// <summary>
        /// TokenEndpoint
        /// </summary>
        public string TokenEndpoint { get; set; }

        /// <summary>
        /// ResourceServerEndpoint
        /// </summary>
        public string ResourceServerEndpoint { get; set; }

        /// <summary>
        /// UseMockServices
        /// </summary>
        public bool UseMockServices { get; set; }
        /// <summary>
        /// Skip Tin validation in core
        /// </summary>
        public bool SkipCheckTin { get; set; }
        /// <summary>
        /// Clients
        /// </summary>
        public List<Credentials> Clients { get; set; } = new List<Credentials>();


        /// <summary>
        /// The GovGr Kyc Client details.
        /// </summary>
        public class Credentials
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// ClientId
            /// </summary>
            public string ClientId { get; set; }

            /// <summary>
            /// ClientSecret
            /// </summary>
            public string ClientSecret { get; set; }

            /// <summary>
            /// RedirectUri
            /// </summary>
            public string RedirectUri { get; set; }
        }
    }
}

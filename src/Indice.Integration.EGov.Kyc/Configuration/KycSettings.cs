using System.Collections.Generic;

namespace Indice.Integration.EGov.Kyc.Configuration
{
    /// <summary>
    /// The Kyc settings.
    /// </summary>
    public class KycSettings
    {
        /// <summary>
        /// The name of the AppSetting
        /// </summary>
        public const string Name = nameof(KycSettings);

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
        public List<EGovKycClient> Clients { get; set; } = new List<EGovKycClient>();
    }
}

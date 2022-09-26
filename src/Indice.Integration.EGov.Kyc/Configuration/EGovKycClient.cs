namespace Indice.Integration.EGov.Kyc.Configuration
{
    /// <summary>
    /// The GovGr Kyc Client details.
    /// </summary>
    public class EGovKycClient
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
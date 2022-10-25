using System;
using System.Linq;
using System.Net.Http;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr
{
    /// <summary>
    /// GovGR Http Client 
    /// </summary>
    public class GovGrClient {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<GovGrSettings> _settings;
        private readonly GovGrKycScopeDescriber _kycScopeDescriber;

        /// <summary>
        /// Create a GovGrClient
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="settings"></param>
        /// <param name="kycScopeDescriber"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public GovGrClient(IHttpClientFactory httpClientFactory,
            IOptions<GovGrSettings> settings,
            GovGrKycScopeDescriber kycScopeDescriber) {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _kycScopeDescriber = kycScopeDescriber ?? throw new ArgumentNullException(nameof(kycScopeDescriber));
        }

        internal GovGrSettings Settings => _settings.Value;

        /// <summary>
        /// Access the KYC API
        /// </summary>
        /// <param name="clientCredentials">Client credentials</param>
        /// <returns></returns>
        public IKycService Kyc(GovGrSettings.Credentials clientCredentials) => new GovGrKycClient(_httpClientFactory.CreateClient(nameof(GovGrClient)), _settings, _kycScopeDescriber, clientCredentials);

        /// <summary>
        /// Access the Wallet API
        /// </summary>
        /// <param name="clientCredentials">Client credentials</param>
        /// <returns></returns>
        public IWalletService Wallet(GovGrSettings.Credentials clientCredentials) => throw new NotImplementedException();
    }

    /// <summary>
    /// Extensions on the <see cref="GovGrClient"/>
    /// </summary>
    public static class GovGrClientExtensions
    {


        /// <summary>
        /// Access the KYC through a convention in the settings. This is a hack.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public static IKycService Kyc(this GovGrClient client, string clientName) {
            if (string.IsNullOrEmpty(clientName)) {
                throw new ArgumentOutOfRangeException(nameof(clientName));
            }
            var credentials = client.Settings.Clients.FirstOrDefault(x => clientName.Equals(x.Name, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception($"Client with client name \"{clientName}\" not found");
            return client.Kyc(credentials);
        }
    }
}

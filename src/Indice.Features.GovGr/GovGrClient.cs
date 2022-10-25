using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime;
using System.Text;
using Indice.Features.GovGr;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;

namespace Indice.Features.Kyc.GovGr
{
    /// <summary>
    /// GovGR Http Client 
    /// </summary>
    public class GovGrClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<GovGrKycSettings> _settings;
        private readonly GovGrKycScopeDescriber _kycScopeDescriber;

        /// <summary>
        /// Create a GovGrClient
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="settings"></param>
        /// <param name="kycScopeDescriber"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public GovGrClient(IHttpClientFactory httpClientFactory, 
            IOptions<GovGrKycSettings> settings,
            GovGrKycScopeDescriber kycScopeDescriber) {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _kycScopeDescriber = kycScopeDescriber ?? throw new ArgumentNullException(nameof(kycScopeDescriber));
        }

        /// <summary>
        /// Access the KYC API
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public IKycService KYC(string clientName) => new GovGrKycClient(_httpClientFactory.CreateClient(nameof(GovGrClient)), _settings, _kycScopeDescriber, clientName);

        /// <summary>
        /// Access the KYC API
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public IWalletService Wallet(string clientName) => throw new NotImplementedException();
    }
}

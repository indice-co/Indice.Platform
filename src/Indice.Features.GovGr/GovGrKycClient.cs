using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using Indice.Extensions;
using Indice.Features.GovGr;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Models;
using Indice.Features.GovGr.Types;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr
{
    /// <inheritdoc />
    internal class GovGrKycClient : IKycService
    {
        private const string FQDN_DEMO = "kycdemo.gsis.gr";
        private const string FQDN_STAGE = "kyc-stage.gov.gr";
        private const string FQDN_PROD = "kyc.gov.gr";
        private readonly HttpClient _httpClient;
        private readonly GovGrKycScopeDescriber _govGrKycScopeDescriber;
        private readonly GovGrOptions.KycOptions _settings;

        internal GovGrKycClient(
            HttpClient httpClient,
            GovGrKycScopeDescriber govGrKycScopeDescriber,
            GovGrOptions.KycOptions settings) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _govGrKycScopeDescriber = govGrKycScopeDescriber ?? throw new ArgumentNullException(nameof(govGrKycScopeDescriber));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            if (_httpClient.BaseAddress is null) {
                _httpClient.BaseAddress = BaseAddress;
            }
        }


        protected string BaseDomain => _settings.IsStaging ? FQDN_STAGE : 
                                       _settings.IsDevelopment ? FQDN_DEMO : FQDN_PROD;
        protected Uri BaseAddress => new($"https://{BaseDomain}");

        public List<ScopeDescription> GetAvailableScopes(IStringLocalizer localizer = null) => _govGrKycScopeDescriber.GetDescriptions(localizer);

        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        public async Task<KycPayload> GetDataAsync(string code) {
            if (_settings.IsMock)
                return JsonSerializer.Deserialize<KycPayload>(GovGrConstants.KycMockJsonString);

            if (code is null) { throw new ArgumentNullException(nameof(code)); }

            // exchange authorization code with access token, using basic authentication 
            var accessToken = await GetAccessToken(code);
            
            _httpClient.SetBearerToken(accessToken);
            var httpResponse = await _httpClient.GetAsync("/1/data");
            var response = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode) {
                throw new GovGrServiceException(response);
            }
            var encodedResponse = JsonSerializer.Deserialize<KycHttpResponse>(response);
            var jsonString = encodedResponse.Payload.Base64UrlSafeDecode();
            return JsonSerializer.Deserialize<KycPayload>(jsonString);
        }

        /// <summary>
        /// Get Access Token from EGovKyc Identity server
        /// </summary>
        private async Task<string> GetAccessToken(string code) {
            // https://en.wikipedia.org/wiki/Basic_access_authentication
            //_httpClient.SetBasicAuthenticationOAuth(_settings.Credentials.ClientId, _settings.Credentials.ClientSecret);

            var tokenResponse = await _httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest {
                Address = "/oauth/token",
                ClientId = _settings.ClientId,
                ClientSecret = _settings.ClientSecret,
                RedirectUri = _settings.RedirectUri,
                Code = code,
            });
            if (tokenResponse.IsError) {
                throw new GovGrServiceException(tokenResponse.Error);
            }
            return tokenResponse.AccessToken;
        }
    }
}

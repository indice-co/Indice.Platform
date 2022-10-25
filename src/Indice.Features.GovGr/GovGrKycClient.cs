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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr
{
    /// <inheritdoc />
    internal class GovGrKycClient : IKycService
    {
        private readonly HttpClient _httpClient;
        private readonly GovGrKycScopeDescriber _govGrKycScopeDescriber;
        private readonly GovGrSettings _settings;

        internal GovGrKycClient(
            HttpClient httpClient,
            IOptions<GovGrSettings> settings,
            GovGrKycScopeDescriber govGrKycScopeDescriber,
            GovGrSettings.Credentials clientCredentials) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _govGrKycScopeDescriber = govGrKycScopeDescriber ?? throw new ArgumentNullException(nameof(govGrKycScopeDescriber));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            Credentials = clientCredentials ?? throw new ArgumentNullException(nameof(clientCredentials));
        }

        protected GovGrSettings.Credentials Credentials { get; }

        public List<ScopeDescription> GetAvailableScopes(IStringLocalizer localizer = null) => _govGrKycScopeDescriber.GetDescriptions(localizer);

        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        public async Task<KycPayload> GetData(string code) {
            if (_settings.UseMockServices)
                return JsonSerializer.Deserialize<KycPayload>(GovGrConstants.KycMockJsonString);

            if (code is null) { throw new ArgumentNullException(nameof(code)); }

            // exchange authorization code with access token, using basic authentication 
            var accessToken = await GetAccessToken(_settings.TokenEndpoint, Credentials.ClientId, Credentials.ClientSecret, code, Credentials.RedirectUri);
            // get data from resource server
            return await GetEGovKycResponsePayload(accessToken, _settings.ResourceServerEndpoint);
        }

        /// <summary>
        /// Get Access Token from EGovKyc Identity server
        /// </summary>
        private async Task<string> GetAccessToken(string tokenEndpoint, string clientId, string clientSecret, string code, string redirectUri) {
            var tokenClient = _httpClient;

            // https://en.wikipedia.org/wiki/Basic_access_authentication
            var credentials = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
            tokenClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + Convert.ToBase64String(credentials));

            var tokenResponse = await tokenClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest {
                Address = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Code = code,
                RedirectUri = redirectUri,
            });

            return tokenResponse.AccessToken;
        }

        /// <summary>
        /// Get EGovKycResponsePayload from EGovKyc Resource server
        /// </summary>
        private async Task<KycPayload> GetEGovKycResponsePayload(string accessToken, string resourceServerEndpoint) {
            var apiClient = _httpClient;

            apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
            var httpResponse = await apiClient.GetAsync(resourceServerEndpoint);
            var response = await httpResponse.Content.ReadAsStringAsync();
            var encodedResponse = JsonSerializer.Deserialize<KycHttpResponse>(response);
            var jsonString = encodedResponse.Payload.Base64UrlSafeDecode();
            return JsonSerializer.Deserialize<KycPayload>(jsonString);
        }
    }
}

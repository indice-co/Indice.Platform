using System;
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
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr
{
    /// <inheritdoc />
    public class GovGrKycClient : IKycService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GovGrKycSettings _settings;

        public GovGrKycClient(
            IHttpClientFactory httpClientFactory,
            IOptions<GovGrKycSettings> settings
            ) {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        public async Task<KycPayload> GetData(string clientName, string code) {
            if (_settings.UseMockServices)
                return JsonSerializer.Deserialize<KycPayload>(GovGrConstants.KycMockJsonString);

            if (clientName is null) { throw new ArgumentNullException(nameof(clientName)); }
            if (code is null) { throw new ArgumentNullException(nameof(code)); }

            var clientSettings = _settings.Clients.FirstOrDefault(x => x.Name == clientName)
                                 ?? throw new Exception($"Client with name {clientName} not found");

            // exchange authorization code with access token, using basic authentication 
            var accessToken = await GetAccessToken(_settings.TokenEndpoint, clientSettings.ClientId, clientSettings.ClientSecret, code, clientSettings.RedirectUri);
            // get data from resource server
            return await GetEGovKycResponsePayload(accessToken, _settings.ResourceServerEndpoint);
        }

        /// <summary>
        /// Get Access Token from EGovKyc Identity server
        /// </summary>
        private async Task<string> GetAccessToken(string tokenEndpoint, string clientId, string clientSecret, string code, string redirectUri) {
            var tokenClient = _httpClientFactory.CreateClient(nameof(GovGrKycClient));

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
            var apiClient = _httpClientFactory.CreateClient(nameof(GovGrKycClient));

            apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
            var httpResponse = await apiClient.GetAsync(resourceServerEndpoint);
            var response = await httpResponse.Content.ReadAsStringAsync();
            var encodedResponse = JsonSerializer.Deserialize<KycHttpResponse>(response);
            var jsonString = encodedResponse.Payload.Base64UrlSafeDecode();
            return JsonSerializer.Deserialize<KycPayload>(jsonString);
        }
    }
}

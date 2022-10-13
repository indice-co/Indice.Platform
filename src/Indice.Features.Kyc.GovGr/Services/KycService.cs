using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel.Client;
using Indice.Features.Kyc.GovGr.Configuration;
using Indice.Features.Kyc.GovGr.Interfaces;
using Indice.Features.Kyc.GovGr.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Kyc.GovGr.Services
{
    /// <inheritdoc />
    public class KycService : IKycService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly KycSettings _settings;

        public KycService(
            IHttpClientFactory httpClientFactory,
            IOptions<KycSettings> settings
            ) {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Get Data from eGov KYC
        /// </summary>
        public async Task<EGovKycResponsePayload> GetData(string clientName, string code) {
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
            var tokenClient = _httpClientFactory.CreateClient(nameof(KycService));

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
        private async Task<EGovKycResponsePayload> GetEGovKycResponsePayload(string accessToken, string resourceServerEndpoint) {
            var apiClient = _httpClientFactory.CreateClient(nameof(KycService));

            apiClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
            var httpResponse = await apiClient.GetAsync(resourceServerEndpoint);
            var response = await httpResponse.Content.ReadAsStringAsync();
            var encodedResponse = JsonSerializer.Deserialize<KycResponse>(response);
            // for some reason we need the help of this wrapper for successful decode, check: https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
            var jsonString = Decode(encodedResponse.payload);

            return JsonSerializer.Deserialize<EGovKycResponsePayload>(jsonString);
        }

        #region helpers

        private static string Decode(string text) {
            text = text.Replace('_', '/').Replace('-', '+');
            switch (text.Length % 4) {
                case 2:
                    text += "==";
                    break;
                case 3:
                    text += "=";
                    break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }

        #endregion

    }
}

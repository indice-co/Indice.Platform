using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Models;
using static Indice.Features.GovGr.Configuration.GovGrOptions;
using System.Net.Mime;
using Indice.Features.GovGr.Types;

namespace Indice.Features.GovGr
{
    internal class GovGrDocumentsClient : IDocumentsService
    {
        private const string BASE_URL_MASK = "https://dilosi.services.gov.gr/api/gates/{0}/gate/documents/";

        private readonly HttpClient _httpClient;
        private readonly DocumentsOptions _settings;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        protected string ServiceName => _settings.ServiceName;
        protected Uri BaseAddress => new(string.Format(BASE_URL_MASK, ServiceName));

        public GovGrDocumentsClient(
            HttpClient httpClient,
            DocumentsOptions settings) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (_httpClient.BaseAddress is null) {
                _httpClient.BaseAddress = BaseAddress;
            }
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {_settings.Token}");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        }

        public async Task<DoucumentsReponse> PostAsync(DocumentData walletRequest) {
            if (walletRequest is null)
                throw new ArgumentNullException(nameof(walletRequest));

            // populate service name ref
            walletRequest.Document.Template.RefName = ServiceName;

            var response = await _httpClient.PostAsync("", new StringContent(
                JsonSerializer.Serialize(walletRequest, _jsonSerializerOptions),
                Encoding.UTF8,
                MediaTypeNames.Application.Json));

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                throw new GovGrServiceException(body);
            }
            return JsonSerializer.Deserialize<DoucumentsReponse>(body, _jsonSerializerOptions);
        }
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ResourceOwnerPasswordFlow.Models;

namespace ResourceOwnerPasswordFlow.Services
{
    public class IdentityApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdentityApiService> _logger;

        public IdentityApiService(HttpClient httpClient, ILogger<IdentityApiService> logger) {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SummaryInfo> GetSystemSummary() {
            var httpResponseMessage = await _httpClient.GetAsync("api/dashboard/summary");
            var result = default(SummaryInfo);
            if (!httpResponseMessage.IsSuccessStatusCode) {
                _logger.LogError($"Endpoint '{httpResponseMessage.RequestMessage.RequestUri}' responded with an error status code: '{httpResponseMessage.StatusCode}' with reason: '{httpResponseMessage.ReasonPhrase}'.");
                return result;
            }
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            result = JsonConvert.DeserializeObject<SummaryInfo>(content);
            return result;
        }
    }
}

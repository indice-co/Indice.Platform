using System.Net.Http.Headers;
using System.Net;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Messages.Core;
using Indice.Security;
using Indice.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using IdentityModel.Client;
using System.Text.Json;
using Indice.Types;
using static IdentityModel.ClaimComparer;
using System.Web;

namespace Indice.Features.Cases.Core.Services;
internal class CustomerIdentityResolverService : ICustomerIntegrationService
{

    private HttpClient _httpClient { get; }
    private CustomerIdentityResolverOptions _options { get; }
    private IDistributedCache _cache { get; }

    private const string TOKEN_CACHE_KEY = "campaigns_id_contact_resolver_token";

    /// <summary>Creates a new instance of <see cref="ICustomerIntegrationService"/>.</summary>
    public CustomerIdentityResolverService(
        HttpClient httpClient,
        IOptions<CustomerIdentityResolverOptions> options,
        IDistributedCache cache
    ) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<CustomerData> GetCustomerData(string customerId, string caseTypeCode) {
        if (string.IsNullOrWhiteSpace(customerId)) {
            return default;
        }
        var accessToken = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync($"api/users/{customerId}");
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return default;
        }
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var identityUser = JsonSerializer.Deserialize<IdentityUserSingleResponse>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings())!;

        return new CustomerData { FormData = new { UserName = identityUser.Email } };
    }

    //public virtual string Get

    public async Task<List<CustomerDetails>> GetCustomers(SearchCustomerCriteria criteria) {
        var accessToken = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var uriBuilder = new UriBuilder("api/users") {
            Port = -1,
            Scheme = string.Empty
        };
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
        queryString[nameof(ListOptions.Page)] = "1";
        if (!string.IsNullOrEmpty(criteria.CustomerId))
            queryString[nameof(ListOptions.Search)] = criteria.CustomerId;
        queryString[nameof(ListOptions.Size)] = "100";

        uriBuilder.Query = queryString.ToString();
        var response = await _httpClient.GetAsync($"/{uriBuilder}");
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var identityUserList = JsonSerializer.Deserialize<ResultSet<IdentityUserListItemResponse>>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings())!;

        return identityUserList.Items.Select(x => new CustomerDetails() {
            UserId = x.Id,
            CustomerId = x.Id,
            FirstName = x.FirstName,
            LastName = x.LastName,
            Metadata = x.Claims.ToDictionary(claim => claim.Type, claim => claim.Value)
        }).ToList();
    }


    private async Task<string> GetAccessToken() {
        var accessToken = await _cache.GetStringAsync(TOKEN_CACHE_KEY);
        if (!string.IsNullOrWhiteSpace(accessToken)) {
            return accessToken;
        }
        var response = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest {
            Address = $"{_options.BaseAddress}connect/token",
            ClientId = _options.ClientId!,
            ClientSecret = _options.ClientSecret,
            Scope = "identity identity:users"
        });
        if (response.IsError) {
            throw response.Exception!;
        }
        accessToken = response.AccessToken;
        await _cache.SetStringAsync(TOKEN_CACHE_KEY, accessToken!, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 60)
        });
        return accessToken!;
    }

    private sealed record IdentityUserSingleResponse(string Id, string? Email, string? PhoneNumber, IEnumerable<IdentityUserClaimResponse> Claims);

    private sealed record IdentityUserListItemResponse(string Id, string? FirstName, string? LastName, string? Email, string? PhoneNumber)
    {
        public IEnumerable<IdentityUserClaimResponse> Claims { get; set; } = [];
    }

    private sealed record IdentityUserClaimResponse(int Id, string? Type, string? Value);
}

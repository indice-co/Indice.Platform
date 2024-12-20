using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Web;
using IdentityModel.Client;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;
internal class ContactProviderIdentityServer : IContactProvider
{

    private readonly HttpClient _httpClient;
    private readonly CasesOptions _caseOptions;
    private readonly ContactProviderIdentityOptions _options;
    private readonly IDistributedCache _cache;

    private const string TOKEN_CACHE_KEY = "campaigns_id_contact_resolver_token";

    /// <summary>Creates a new instance of <see cref="IContactProvider"/>.</summary>
    public ContactProviderIdentityServer(
        HttpClient httpClient,
        IOptions<CasesOptions> caseOptions,
        IOptions<ContactProviderIdentityOptions> options,
        IDistributedCache cache
    ) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _caseOptions = caseOptions?.Value ?? throw new ArgumentNullException(nameof(caseOptions));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }


    public async Task<ResultSet<Contact>> GetListAsync(ClaimsPrincipal user, ListOptions<ContactFilter> listOptions) {
        var accessToken = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var uriBuilder = new UriBuilder("api/users") {
            Port = -1,
            Scheme = string.Empty
        };
        
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
        queryString[nameof(ListOptions.Page)] = listOptions.Page.ToString();
        queryString[nameof(ListOptions.Search)] = listOptions.Filter.Reference ?? listOptions.Search;
        queryString[nameof(ListOptions.Size)] = listOptions.Size.ToString();
        queryString.Add("expandClaims", BasicClaimTypes.Tin); // will include the tax id in the results
        queryString.Add("expandClaims", _caseOptions.GroupIdClaimType); // will include the tax id in the results
        uriBuilder.Query = queryString.ToString();
        var response = await _httpClient.GetAsync($"/{uriBuilder}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ResultSet<IdentityUserListItemResponse>>(JsonSerializerOptionDefaults.GetDefaultSettings());
        return result!.Items.Select(x => x.ToContact(_caseOptions)).ToResultSet(result.Count);
    }

    public async Task<Contact?> GetByReferenceAsync(ClaimsPrincipal user, string reference, string caseTypeCode) {
        if (string.IsNullOrWhiteSpace(reference)) {
            return default;
        }
        var accessToken = await GetAccessToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync($"api/users/{reference}");
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return default;
        }
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IdentityUserSingleResponse>(JsonSerializerOptionDefaults.GetDefaultSettings());

        return result?.ToContact(_caseOptions);
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
        if (response.IsError && response.Exception is not null) {
            throw response.Exception!;
        } else if (response.IsError) {
            throw new Exception(response.Error);
        }
        accessToken = response.AccessToken;
        await _cache.SetStringAsync(TOKEN_CACHE_KEY, accessToken!, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 60)
        });
        return accessToken!;
    }

    private sealed record IdentityUserSingleResponse(string Id, string? Email, string? PhoneNumber, List<IdentityUserClaimResponse> Claims) 
    {

        public Contact ToContact(CasesOptions casesOptions) => new() {
            UserId = Id,
            Reference = Claims.Find(x => x.Type == casesOptions.ReferenceIdClaimType)?.Value ?? Id,
            Email = Email,
            PhoneNumber = PhoneNumber,
            FirstName = Claims.Find(x => x.Type == BasicClaimTypes.GivenName)?.Value,
            LastName = Claims.Find(x => x.Type == BasicClaimTypes.FamilyName)?.Value,
            GroupId = Claims.Find(x => x.Type == casesOptions.GroupIdClaimType)?.Value,
            Tin = Claims.Find(x => x.Type == casesOptions.TinClaimType)?.Value,
            Metadata = Claims.ToLookup(x => x.Type)
                             .ToDictionary(x => x.Key, x => string.Join(',', x))
        };
    }

    private sealed record IdentityUserListItemResponse(string Id, string? FirstName, string? LastName, string? Email, string? PhoneNumber)
    {
        public List<IdentityUserClaimResponse> Claims { get; set; } = [];

        public Contact ToContact(CasesOptions casesOptions) => new() {
            UserId = Id,
            Reference = Claims.Find(x => x.Type == casesOptions.ReferenceIdClaimType)?.Value ?? Id,
            Email = Email,
            PhoneNumber = PhoneNumber,
            FirstName = FirstName,
            LastName = LastName,
            GroupId = Claims.Find(x => x.Type == casesOptions.GroupIdClaimType)?.Value,
            Tin = Claims.Find(x => x.Type == casesOptions.TinClaimType)?.Value,
            Metadata = Claims.ToLookup(x => x.Type)
                             .ToDictionary(x => x.Key, x => string.Join(',', x))
        };
    }

    private sealed record IdentityUserClaimResponse(int Id, string Type, string Value);
}

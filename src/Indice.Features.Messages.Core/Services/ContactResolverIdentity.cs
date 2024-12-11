using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Web;
using IdentityModel.Client;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IContactService"/> that gets contact information from Indice API for IdentityServer4.</summary>
public class ContactResolverIdentity : IContactResolver
{
    private const string TOKEN_CACHE_KEY = "campaigns_id_contact_resolver_token";

    /// <summary>Creates a new instance of <see cref="ContactResolverIdentity"/>.</summary>
    public ContactResolverIdentity(
        HttpClient httpClient,
        IOptions<ContactResolverIdentityOptions> options,
        IDistributedCache cache
    ) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private HttpClient HttpClient { get; }
    private ContactResolverIdentityOptions Options { get; }
    private IDistributedCache Cache { get; }

    /// <inheritdoc />
    public async Task<ResultSet<Contact>> Find(ListOptions options) {
        var accessToken = await GetAccessToken();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var uriBuilder = new UriBuilder("api/users") {
            Port = -1,
            Scheme = string.Empty
        };
        var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);
        queryString[nameof(ListOptions.Page)] = options.Page.ToString();
        queryString[nameof(ListOptions.Search)] = options.Search;
        queryString[nameof(ListOptions.Size)] = options.Size.ToString();
        queryString[nameof(ListOptions.Sort)] = options.Sort;
        queryString.Add("expandClaims", BasicClaimTypes.ConsentCommercial);
        queryString.Add("expandClaims", BasicClaimTypes.Locale);
        queryString.Add("expandClaims", BasicClaimTypes.CommunicationPreferences);

        if (Options.HasCustomRecipientId) {
            queryString.Add("expandClaims", Options.UserClaimType);
        }
        uriBuilder.Query = queryString.ToString();
        var response = await HttpClient.GetAsync($"/{uriBuilder}");
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var identityUserList = JsonSerializer.Deserialize<ResultSet<IdentityUserListItemResponse>>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings())!;
        return new ResultSet<Contact> {
            Count = identityUserList.Count,
            Items = identityUserList.Items.Select(identityUser => new Contact {
                RecipientId = Options.HasCustomRecipientId ? (identityUser.Claims?.FirstOrDefault()?.Value ?? identityUser.Id) : identityUser.Id,
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber,
                FirstName = identityUser.FirstName,
                LastName = identityUser.LastName,
                FullName = !string.IsNullOrEmpty(identityUser.FirstName) && !string.IsNullOrEmpty(identityUser.LastName) ? $"{identityUser.FirstName} {identityUser.LastName}" : null,
                CommunicationPreferences = GetCommunicationPreferences(identityUser.Claims),
                Locale = GetLocale(identityUser.Claims),
                ConsentCommercial = GetCommercialConsent(identityUser.Claims)
            })
            .ToArray()
        };
    }

    /// <inheritdoc />
    public async Task<Contact?> Resolve(string? recipientId) {
        if (string.IsNullOrWhiteSpace(recipientId)) {
            return default;
        }
        var accessToken = await GetAccessToken();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await HttpClient.GetAsync($"api/users/{recipientId}");
        if (response.StatusCode == HttpStatusCode.NotFound) {
            return default;
        }
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var identityUser = JsonSerializer.Deserialize<IdentityUserSingleResponse>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings())!;
        var contact = new Contact {
            RecipientId = identityUser.Id,
            Email = identityUser.Email,
            PhoneNumber = identityUser.PhoneNumber,
            FirstName = identityUser.Claims.FirstOrDefault(x => x.Type == BasicClaimTypes.GivenName)?.Value,
            LastName = identityUser.Claims.FirstOrDefault(x => x.Type == BasicClaimTypes.FamilyName)?.Value,
            CommunicationPreferences = GetCommunicationPreferences(identityUser.Claims),
            Locale = GetLocale(identityUser.Claims),
            ConsentCommercial = GetCommercialConsent(identityUser.Claims)
        };
        if (!string.IsNullOrEmpty(contact.FirstName) && !string.IsNullOrEmpty(contact.LastName)) {
            contact.FullName = $"{contact.FirstName} {contact.LastName}";
        }
        return contact;
    }

    private static ContactCommunicationChannelKind GetCommunicationPreferences(IEnumerable<IdentityUserClaimResponse>? claims) {
        if (claims == null)
            return ContactCommunicationChannelKind.Any;
        var communicationPreferences = claims.FirstOrDefault(x => x.Type == BasicClaimTypes.CommunicationPreferences);
        if (communicationPreferences == null)
            return ContactCommunicationChannelKind.Any;
        return Enum.Parse<ContactCommunicationChannelKind>(communicationPreferences.Value!, ignoreCase: true);
    }

    private static string? GetLocale(IEnumerable<IdentityUserClaimResponse>? claims) {
        if (claims == null) return null;
        var userLocale = claims.FirstOrDefault(x => x.Type == BasicClaimTypes.Locale);
        if (userLocale == null)
            return null;
        return userLocale.Value;
    }

    private static bool GetCommercialConsent(IEnumerable<IdentityUserClaimResponse>? claims) {
        if (claims == null) return false;
        var consent = claims.FirstOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercial);
        if (consent == null )
            return false;
        return consent.Value!.Equals(bool.TrueString, StringComparison.CurrentCultureIgnoreCase);
    }

    private async Task<string> GetAccessToken() {
        var accessToken = await Cache.GetStringAsync(TOKEN_CACHE_KEY);
        if (!string.IsNullOrWhiteSpace(accessToken)) {
            return accessToken;
        }
        var response = await HttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest {
            Address = $"{Options.BaseAddress}connect/token",
            ClientId = Options.ClientId!,
            ClientSecret = Options.ClientSecret,
            Scope = "identity identity:users"
        });
        if (response.IsError) {
            throw response.Exception!;
        }
        accessToken = response.AccessToken;
        await Cache.SetStringAsync(TOKEN_CACHE_KEY, accessToken!, new DistributedCacheEntryOptions {
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

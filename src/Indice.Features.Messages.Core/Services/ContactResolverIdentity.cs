using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Duende.IdentityModel.Client;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>An implementation of <see cref="IContactService"/> that gets contact information from Indice API for Identity Server.</summary>
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
    public Task<ResultSet<Contact>> Find(ListOptions options) => FindInternal(options);

    internal async Task<ResultSet<Contact>> FindInternal(ListOptions options, string? recipientId = null) {
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
        if (!string.IsNullOrWhiteSpace(recipientId)) {
            if (Options.HasCustomRecipientId) {
                queryString.Add("claimType", Options.UserClaimType);
                queryString.Add("claimValue", recipientId);
            } else {
                queryString.Add("UserId", recipientId);
            }
        }
        if (Options.HasCustomRecipientId) {
            queryString.Add("expandClaims", Options.UserClaimType);
        }
        foreach (var claim in Options.ClaimsToResolve) {
            queryString.Add("expandClaims", claim);
        }
        uriBuilder.Query = queryString.ToString();
        var response = await HttpClient.GetAsync($"/{uriBuilder}");
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var identityUserList = JsonSerializer.Deserialize<ResultSet<IdentityUserListItemResponse>>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings())!;
        return new ResultSet<Contact> {
            Count = identityUserList.Count,
            Items = identityUserList.Items.Select(identityUser => new Contact {
                RecipientId = Options.HasCustomRecipientId ? (FindClaimValue(identityUser.Claims, Options.UserClaimType) ?? identityUser.Id) : identityUser.Id,
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber,
                FirstName = identityUser.FirstName,
                LastName = identityUser.LastName,
                FullName = !string.IsNullOrEmpty(identityUser.FirstName) && !string.IsNullOrEmpty(identityUser.LastName) ? $"{identityUser.FirstName} {identityUser.LastName}" : null,
                Preference = new ContactPreference {
                    Locale = FindClaimValue(identityUser.Claims, BasicClaimTypes.Locale),
                    ConsentCommercial = GetCommercialConsent(identityUser.Claims),
                    ConsentCommercialDate = GetCommercialConsentDate(identityUser.Claims),
                    DefaultChannels = GetCommunicationPreferences(identityUser.Claims)
                },
                Resolved = true
            })
             .ToArray()
        };
    }

    /// <inheritdoc />
    public async Task<Contact?> Resolve(string? recipientId) {
        if (string.IsNullOrWhiteSpace(recipientId)) {
            return default;
        }
        // in case we have a custom claim for user recipient id we cannot use the get by id endpoint
        // so redirect the call to the find endpoint.
        if (Options.HasCustomRecipientId) {
            return (await FindInternal(new ListOptions(), recipientId)).Items.FirstOrDefault();
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
            FirstName = FindClaimValue(identityUser.Claims, BasicClaimTypes.GivenName),
            LastName = FindClaimValue(identityUser.Claims, BasicClaimTypes.FamilyName),
            Resolved = true,
            LastResolutionDate = DateTimeOffset.UtcNow,
            Preference = new ContactPreference {
                Locale = FindClaimValue(identityUser.Claims, BasicClaimTypes.Locale),
                ConsentCommercial = GetCommercialConsent(identityUser.Claims),
                ConsentCommercialDate = GetCommercialConsentDate(identityUser.Claims),
                DefaultChannels = GetCommunicationPreferences(identityUser.Claims)
            }
        };
        if (!string.IsNullOrEmpty(contact.FirstName) && !string.IsNullOrEmpty(contact.LastName)) {
            contact.FullName = $"{contact.FirstName} {contact.LastName}";
        }
        return contact;
    }

    private static DateTimeOffset? GetCommercialConsentDate(IEnumerable<IdentityUserClaimResponse>? claims) {
        if (claims == null)
            return null;
        var consentDateClaim = claims.FirstOrDefault(x => x.Type == BasicClaimTypes.ConsentCommercialDate);
        if (consentDateClaim == null)
            return null;

        if (!string.IsNullOrWhiteSpace(consentDateClaim.Value) && DateTime.TryParse(consentDateClaim.Value, out var consentDate)) {
            return consentDate;
        }
        return null;
    }

    private static string? FindClaimValue(IEnumerable<IdentityUserClaimResponse>? claims, string claimType) =>
        claims?.FirstOrDefault(x => x.Type == claimType)?.Value;

    private static bool GetCommercialConsent(IEnumerable<IdentityUserClaimResponse>? claims) =>
        claims?.Any(x => x.Type == BasicClaimTypes.ConsentCommercial && bool.TrueString.Equals(x.Value, StringComparison.CurrentCultureIgnoreCase)) ?? false;

    private static List<ContactChannelOption>? GetCommunicationPreferences(IEnumerable<IdentityUserClaimResponse>? claims) {
        if (claims == null)
            return null;
        var communicationPreferences = claims.FirstOrDefault(x => x.Type == BasicClaimTypes.CommunicationPreferences);
        if (communicationPreferences == null)
            return null;
        var enumValues = Enum.Parse<ContactChannelKind>(communicationPreferences.Value!, ignoreCase: true);
        return ContactChannelOption.FromKindFlags(enumValues);
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

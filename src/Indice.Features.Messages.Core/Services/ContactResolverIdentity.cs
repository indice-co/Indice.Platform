﻿using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using IdentityModel.Client;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Security;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services
{
    /// <summary>
    /// An implementation of <see cref="IContactService"/> that gets contact information from Indice API for IdentityServer4.
    /// </summary>
    public class ContactResolverIdentity : IContactResolver
    {
        private const string TOKEN_CACHE_KEY = "campaigns_id_contact_resolver_token";

        /// <summary>
        /// Creates a new instance of <see cref="ContactResolverIdentity"/>.
        /// </summary>
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
        public async Task<ResultSet<Contact>> Find(ListOptions<ContactSearchFilter> options) {
            var accessToken = await GetAccessToken();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await HttpClient.GetAsync($"api/users");
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return null;
        }

        /// <inheritdoc />
        public async Task<Contact> GetById(string id) {
            var accessToken = await GetAccessToken();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var response = await HttpClient.GetAsync($"api/users/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound) {
                return default;
            }
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var identityUser = JsonSerializer.Deserialize<IdentityUserResponse>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings());
            var contact = new Contact {
                Id = Guid.NewGuid(),
                RecipientId = identityUser.Id,
                Email = identityUser.Email,
                PhoneNumber = identityUser.PhoneNumber,
                FirstName = identityUser.Claims.Where(x => x.Type == BasicClaimTypes.GivenName).FirstOrDefault()?.Value,
                LastName = identityUser.Claims.Where(x => x.Type == BasicClaimTypes.FamilyName).FirstOrDefault()?.Value
            };
            if (!string.IsNullOrEmpty(contact.FirstName) && !string.IsNullOrEmpty(contact.LastName)) {
                contact.FullName = $"{contact.FirstName} {contact.LastName}";
            }
            return contact;
        }

        private async Task<string> GetAccessToken() {
            var accessToken = await Cache.GetStringAsync(TOKEN_CACHE_KEY);
            if (!string.IsNullOrWhiteSpace(accessToken)) {
                return accessToken;
            }
            var response = await HttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest {
                Address = $"{Options.BaseAddress}/connect/token",
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                Scope = "identity identity:users"
            });
            if (response.IsError) {
                throw response.Exception;
            }
            accessToken = response.AccessToken;
            await Cache.SetStringAsync(TOKEN_CACHE_KEY, accessToken, new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 60)
            });
            return accessToken;
        }

        private class IdentityUserResponse
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public IEnumerable<IdentityUserClaimResponse> Claims { get; set; } = new List<IdentityUserClaimResponse>();
        }

        private class IdentityUserClaimResponse
        {
            public int Id { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }
    }
}

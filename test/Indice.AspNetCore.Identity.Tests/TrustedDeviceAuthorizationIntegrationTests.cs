using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.AspNetCore.Identity.Tests
{
    public class TrustedDeviceAuthorizationIntegrationTests
    {
        // Constants
        private const string BaseUrl = "https://server";
        private const string ClientId = "ppk-client";
        private const string ClientSecret = "JUEKX2XugFv5XrX3";
        // Private fields
        private readonly string _deviceRegistrationInitiationUrl = $"{BaseUrl}/my/devices/register/init";
        private readonly Guid _deviceId = Guid.Parse("bf9856a0-5778-43d1-99d9-a29fe39e04e9");
        private readonly HttpClient _httpClient;

        public TrustedDeviceAuthorizationIntegrationTests() {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(services => {
                services.AddSingleton<ITotpService, MockTotpService>();
                services.AddIdentityServer(options => {
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(GetIdentityResources())
                .AddInMemoryClients(GetClients())
                .AddTestUsers(GetTestUsers())
                .AddInMemoryPersistedGrants()
                .AddTrustedDeviceAuthorization()
                .AddDeveloperSigningCredential(persistKey: false);
            });
            builder.Configure(app => {
                app.UseIdentityServer();
            });
            var server = new TestServer(builder);
            var handler = server.CreateHandler();
            _httpClient = new HttpClient(handler) {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        [Fact]
        public async Task CanRegisterDevice() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            await InitiateDeviceRegistrationUsingFingerprint(accessToken);
        }

        private async Task<string> LoginWithPasswordGrant(string userName, string password) {
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var tokenResponse = await _httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scope = $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone}",
                UserName = userName,
                Password = password
            });
            return tokenResponse.AccessToken;
        }

        private async Task InitiateDeviceRegistrationUsingFingerprint(string accessToken) {
            var codeVerifier = GenerateCodeVerifier();
            var data = new Dictionary<string, string> {
                { "mode", "fingerprint" },
                { "device_id", _deviceId.ToString() },
                { "code_challenge", GenerateCodeChallenge(codeVerifier) },
                { "code_challenge_method", "S256" }
            };
            var form = new FormUrlEncodedContent(data);
            _httpClient.SetBearerToken(accessToken);
            var response = await _httpClient.PostAsync(_deviceRegistrationInitiationUrl, form);
            if (!response.IsSuccessStatusCode) { }
        }

        private static string GenerateCodeVerifier() => CryptoRandom.CreateUniqueId(32);

        private static string GenerateCodeChallenge(string codeVerifier) {
            string codeChallenge;
            using (var sha256 = SHA256.Create()) {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64Url.Encode(challengeBytes);
            }
            return codeChallenge;
        }

        #region IdentityServer Configuration
        private static IEnumerable<Client> GetClients() => new List<Client> {
            new Client {
                ClientId = ClientId,
                ClientName = "Public/Private key client",
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                ClientSecrets = {
                    new Secret(ClientSecret.ToSha256())
                },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Phone
                },
                RequireConsent = false,
                RequirePkce = false,
                RequireClientSecret = true,
                AllowOfflineAccess = true,
                AlwaysSendClientClaims = true,
                Claims = {
                    new ClientClaim(BasicClaimTypes.TrustedDevice, "true", ClaimValueTypes.Boolean)
                }
            }
        };

        private static IEnumerable<IdentityResource> GetIdentityResources() => new List<IdentityResource> {
            new IdentityResources.OpenId(),
            new IdentityResources.Phone()
        };

        private static List<TestUser> GetTestUsers() => new() {
            new TestUser {
                SubjectId = "123456",
                Username = "alice",
                Password = "alice",
                Claims = {
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "alice_smith@example.com"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "69XXXXXXXX"),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, "true", ClaimValueTypes.Boolean)
                }
            },
            new TestUser {
                SubjectId = "654321",
                Username = "bob",
                Password = "bob",
                Claims = {
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "bob_smith@email.com"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "69XXXXXXXX"),
                    new Claim(JwtClaimTypes.PhoneNumberVerified, "false", ClaimValueTypes.Boolean)
                }
            }
        };
        #endregion
    }
}

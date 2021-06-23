using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Indice.AspNetCore.Identity.Tests.Models;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Indice.AspNetCore.Identity.Tests
{
    public class TrustedDeviceAuthorizationIntegrationTests
    {
        #region Keys
        private const string PrivateKey =
            @"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
";
        private const string PublicKey =
            @"MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAt/o5fhsMxUh5jdsqyHCG
2VNMEtDnL7D0e5kUvZcqYCoGKD6yeHffRgGJQfw1XZV0THUxPjy3grp+UfP5VN6Q
CNfZ3rgFVXMvP1j2t+AX6l/1wXefTsH3V6233KYt9Yg0M7v4q/wzPAhrifHluiVV
75dgcc7bpHcjLLhmhq7h2f2l9ehX3UYRb0/JNuBsM/Vs5pumHOKFRQX0ojE5uyL1
seWMUX5C1lnzSgmEljzNejYGx6vtoXr8TwPdJCmCQwyoHbu8tiY0OMktUNd4aaOf
ANpiWAZ8+DmegU8Ttf2AkPkSt5s9uesRoY4nREURmufLmufu9B3BCW1fIqw6rRdF
yWHSPYaiT8wQxVP1Z5IefAsR6MCVRt9zGLE3E0eeEAy8uyfgZ5X/5emNH5Ket6nQ
a89Krip2rKNLc9QhLwDya2ApsCho7KjvmuKsaYq+rP7lwi9isxknVK7LnKRcUv37
5MjMbOwpfvFIkbZ31GEZPh6HQFxwdqFl3jQRbLoDcVy8uKVh64ooiTo8BoC4GfEv
Ge/4hfWMdoAJCeg1kLnKxu/Tb43jnQUtMqy+4SQKueI5apb9aSpLhQEnbzLfuUoK
Og94L9TDq7oB4H+k+HUVGEF8tIpOeBg2jQV4VfwGZQWDvraDmGEGfnSsjBp5cZ2+
GIgPv61gfW36ROqVBBU/A9UCAwEAAQ==
";
        #endregion
        // Constants
        private const string BaseUrl = "https://server";
        private const string ClientId = "ppk-client";
        private const string ClientSecret = "JUEKX2XugFv5XrX3";
        private const string DevicePin = "4412";
        // Private fields
        private readonly string _deviceRegistrationInitiationUrl = $"{BaseUrl}/my/devices/register/init";
        private readonly string _deviceRegistrationCompletionUrl = $"{BaseUrl}/my/devices/register/complete";
        private readonly string _deviceAuthorizationUrl = $"{BaseUrl}/my/devices/connect/authorize";
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _output;

        public TrustedDeviceAuthorizationIntegrationTests(ITestOutputHelper output) {
            _output = output;
            var builder = new WebHostBuilder();
            builder.ConfigureServices(services => {
                services.AddSingleton<ITotpService, MockTotpService>();
                services.AddIdentityServer(options => {
                    options.EmitStaticAudienceClaim = true;
                })
                .AddInMemoryIdentityResources(GetIdentityResources())
                .AddInMemoryApiScopes(GetApiScopes())
                .AddInMemoryApiResources(GetApiResources())
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
        public async Task<string> Can_Register_New_Device_Using_Fingerprint() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            return deviceId;
        }

        [Fact]
        public async Task<string> Can_Register_Device_Using_Pin_When_Already_Supports_Fingerprint() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            codeVerifier = GenerateCodeVerifier();
            challenge = await InitiateDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId);
            response = await CompleteDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            return deviceId;
        }

        [Fact]
        public async Task<string> Can_Register_New_Device_Using_Pin() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            return deviceId;
        }

        [Fact]
        public async Task<string> Can_Register_Device_Using_Fingerprint_When_Already_Supports_Pin() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            codeVerifier = GenerateCodeVerifier();
            challenge = await InitiateDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId);
            response = await CompleteDeviceRegistrationUsingFingerprint(accessToken, codeVerifier, deviceId, challenge);
            if (!response.IsSuccessStatusCode) {
                var responseJson = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseJson);
            }
            Assert.True(response.IsSuccessStatusCode);
            return deviceId;
        }

        [Fact]
        public async Task Can_Authorize_Existing_Device_Using_Fingerprint() {
            var deviceId = await Can_Register_Device_Using_Fingerprint_When_Already_Supports_Pin();
            var codeVerifier = GenerateCodeVerifier();
            var challenge = await InitiateDeviceAuthorizationUsingFingerprint(codeVerifier, deviceId);
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var x509SigningCredentials = GetX509SigningCredentials();
            var signature = SignMessage(challenge, x509SigningCredentials);
            var tokenResponse = await _httpClient.RequestTokenAsync(new TokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                GrantType = CustomGrantTypes.TrustedDevice,
                Parameters = {
                    { "code", challenge },
                    { "code_signature", signature },
                    { "code_verifier", codeVerifier },
                    { "device_id", deviceId },
                    { "public_key", PublicKey },
                    { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
                }
            });
            Assert.False(tokenResponse.IsError);
        }

        [Fact]
        public async Task Can_Authorize_Existing_Device_Using_Pin() {
            var deviceId = await Can_Register_Device_Using_Pin_When_Already_Supports_Fingerprint();
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var tokenResponse = await _httpClient.RequestTokenAsync(new TokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                GrantType = CustomGrantTypes.TrustedDevice,
                Parameters = {
                    { "device_id", deviceId },
                    { "pin", DevicePin },
                    { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
                }
            });
            Assert.False(tokenResponse.IsError);
        }

        #region Helper Methods
        private async Task<string> InitiateDeviceAuthorizationUsingFingerprint(string codeVerifier, string deviceId) {
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var data = new Dictionary<string, string> {
                { "client_id", ClientId },
                { "code_challenge", codeChallenge },
                { "device_id", deviceId },
                { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
            };
            var form = new FormUrlEncodedContent(data);
            var response = await _httpClient.PostAsync(_deviceAuthorizationUrl, form);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
                throw new HttpRequestException();
            }
            var result = JsonSerializer.Deserialize<TrustedDeviceAuthorizationResultDto>(responseJson);
            return result.Challenge;
        }

        private async Task<string> LoginWithPasswordGrant(string userName, string password) {
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var tokenResponse = await _httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Scope = $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1",
                UserName = userName,
                Password = password
            });
            return tokenResponse.AccessToken;
        }

        private Task<string> InitiateDeviceRegistrationUsingFingerprint(string accessToken, string codeVerifier, string deviceId) =>
            InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "fingerprint");

        private Task<string> InitiateDeviceRegistrationUsingPin(string accessToken, string codeVerifier, string deviceId) =>
            InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "pin");

        private async Task<string> InitiateDeviceRegistration(string accessToken, string codeVerifier, string deviceId, string mode) {
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var data = new Dictionary<string, string> {
                { "code_challenge", codeChallenge },
                { "device_id", deviceId },
                { "mode", mode }
            };
            var form = new FormUrlEncodedContent(data);
            _httpClient.SetBearerToken(accessToken);
            var response = await _httpClient.PostAsync(_deviceRegistrationInitiationUrl, form);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
                throw new HttpRequestException();
            }
            var result = JsonSerializer.Deserialize<TrustedDeviceAuthorizationResultDto>(responseJson);
            return result.Challenge;
        }

        private Task<HttpResponseMessage> CompleteDeviceRegistrationUsingFingerprint(string accessToken, string codeVerifier, string deviceId, string challenge) =>
            CompleteDeviceRegistration(accessToken, codeVerifier, deviceId, challenge, "fingerprint");

        private Task<HttpResponseMessage> CompleteDeviceRegistrationUsingPin(string accessToken, string codeVerifier, string deviceId, string challenge) =>
            CompleteDeviceRegistration(accessToken, codeVerifier, deviceId, challenge, "pin");

        private async Task<HttpResponseMessage> CompleteDeviceRegistration(string accessToken, string codeVerifier, string deviceId, string challenge, string mode) {
            var x509SigningCredentials = GetSigningCredentials();
            var signature = Sign(challenge, x509SigningCredentials);
            var data = new Dictionary<string, string> {
                { "code", challenge },
                { "code_signature", signature },
                { "code_verifier", codeVerifier },
                { "device_id", deviceId },
                { "device_name", "George OnePlus 7 Pro" },
                { "device_platform", $"{DevicePlatform.Android}" },
                { "otp", "123456" }
            };
            if (mode == "fingerprint") {
                data.Add("public_key", PublicKey);
            }
            if (mode == "pin") {
                data.Add("pin", DevicePin);
            }
            var form = new FormUrlEncodedContent(data);
            _httpClient.SetBearerToken(accessToken);
            return await _httpClient.PostAsync(_deviceRegistrationCompletionUrl, form);
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

        private static X509SigningCredentials GetX509SigningCredentials() {
            var certificate = X509Certificate2.CreateFromPem(PublicKey, PrivateKey);
            var signingCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSha256Signature);
            return signingCredentials;
        }

        private static SigningCredentials GetSigningCredentials() {
            var privateKey = Convert.FromBase64String(PrivateKey);
            using (var rsa = RSA.Create()) {
                rsa.ImportRSAPrivateKey(privateKey, out _);
                var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256) {
                    CryptoProviderFactory = new CryptoProviderFactory {
                        CacheSignatureProviders = false
                    }
                };
                return signingCredentials;
            }
        }

        private static string Sign(string message, SigningCredentials signingCredentials) {
            // Create a UnicodeEncoder to convert between byte array and string.
            ASCIIEncoding ByteConverter = new ASCIIEncoding();

            // Create byte arrays to hold original, encrypted, and decrypted data.
            byte[] originalData = ByteConverter.GetBytes(message);
            byte[] signedData;

            // Create a new instance of the RSACryptoServiceProvider class
            // and automatically create a new key-pair.
            RSACryptoServiceProvider RSAalg = new RSACryptoServiceProvider();
            RSAalg.ImportRSAPrivateKey(Convert.FromBase64String(PrivateKey), out _);
            // Export the key information to an RSAParameters object.
            // You must pass true to export the private key for signing.
            // However, you do not need to export the private key
            // for verification.
            RSAParameters Key = RSAalg.ExportParameters(true);
            // Hash and sign the data.
            signedData = HashAndSignBytes(originalData, Key, HashAlgorithmName.SHA256);

            return Convert.ToBase64String(signedData);
        }

        private static byte[] HashAndSignBytes(byte[] DataToSign, RSAParameters Key, HashAlgorithmName hashAlgorithm) {
            // Create a new instance of RSACryptoServiceProvider using the key from RSAParameters.  
            using (var RSAalg = new RSACryptoServiceProvider()) {
                try {
                    RSAalg.ImportParameters(Key);
                    // Hash and sign the data. Pass a new instance of SHA1CryptoServiceProvider to specify the use of SHA1 for hashing.
                    return RSAalg.SignData(DataToSign, hashAlgorithm, RSASignaturePadding.Pkcs1);
                } catch (CryptographicException e) {
                    Console.WriteLine(e.Message);
                    return null;
                } finally {
                    // Set the key container to be cleared when RSA is garbage collected.
                    RSAalg.PersistKeyInCsp = false;
                }
            }
        }

        private static string SignMessage(byte[] message, X509SigningCredentials x509SigningCredentials) {
            using var key = x509SigningCredentials.Certificate.GetRSAPrivateKey();
            var signedMessage = Convert.ToBase64String(key?.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1) ?? Array.Empty<byte>());
            return signedMessage;
        }

        private static string SignMessage(string message, X509SigningCredentials x509SigningCredentials) {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            return SignMessage(messageBytes, x509SigningCredentials);
        }
        #endregion

        #region IdentityServer Configuration
        private static IEnumerable<Client> GetClients() => new List<Client> {
            new Client {
                ClientId = ClientId,
                ClientName = "Public/Private key client",
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedGrantTypes = {
                    CustomGrantTypes.TrustedDevice,
                    GrantType.ClientCredentials,
                    GrantType.ResourceOwnerPassword
                },
                ClientSecrets = {
                    new Secret(ClientSecret.ToSha256())
                },
                AllowedScopes = {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Phone,
                    "scope1"
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
            new IdentityResources.Phone(),
            new IdentityResources.Email(),
            new IdentityResources.Profile(),
            new IdentityResources.Address()
        };

        private static IEnumerable<ApiScope> GetApiScopes() => new List<ApiScope> {
            new ApiScope(name: "scope1", displayName: "Scope No. 1", userClaims: new string[] {
                JwtClaimTypes.Email,
                JwtClaimTypes.EmailVerified,
                JwtClaimTypes.FamilyName,
                JwtClaimTypes.GivenName,
                JwtClaimTypes.PhoneNumber,
                JwtClaimTypes.PhoneNumberVerified,
                JwtClaimTypes.Subject
            }),
            new ApiScope(name: "scope2", displayName: "Scope No. 2", userClaims: new string[] {
                JwtClaimTypes.Email,
                JwtClaimTypes.PhoneNumber,
                JwtClaimTypes.Subject
            })
        };

        private static IEnumerable<ApiResource> GetApiResources() => new List<ApiResource> {
            new ApiResource(name: "api1", displayName: "API No. 1") {
                Scopes = { "scope1", "scope2" }
            }
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

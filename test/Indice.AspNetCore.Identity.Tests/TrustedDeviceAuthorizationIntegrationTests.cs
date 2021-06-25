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
        // https://www.scottbrady91.com/OpenSSL/Creating-RSA-Keys-using-OpenSSL
        private const string PrivateKey =
            @"-----BEGIN RSA PRIVATE KEY-----
            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
            -----END RSA PRIVATE KEY-----
            ";
        private const string CertificatePublicKey =
            @"-----BEGIN CERTIFICATE-----
            MIIGKTCCBBGgAwIBAgIUZyswHWk9UYHXjUDuFIlTD0+BGpwwDQYJKoZIhvcNAQEL
            BQAwgaMxCzAJBgNVBAYTAkdSMQ8wDQYDVQQIDAZBdHRpY2ExDzANBgNVBAcMBkF0
            aGVuczEUMBIGA1UECgwLSW5kaWNlIEx0ZC4xJjAkBgNVBAsMHVNvZnR3YXJlIERl
            dmVsb3BtZW50IFNlcnZpY2VzMRIwEAYDVQQDDAlpbmRpY2UuZ3IxIDAeBgkqhkiG
            9w0BCQEWEWNvbXBhbnlAaW5kaWNlLmdyMB4XDTIxMDYyNTA5NDM0OVoXDTIyMDYy
            MDA5NDM0OVowgaMxCzAJBgNVBAYTAkdSMQ8wDQYDVQQIDAZBdHRpY2ExDzANBgNV
            BAcMBkF0aGVuczEUMBIGA1UECgwLSW5kaWNlIEx0ZC4xJjAkBgNVBAsMHVNvZnR3
            YXJlIERldmVsb3BtZW50IFNlcnZpY2VzMRIwEAYDVQQDDAlpbmRpY2UuZ3IxIDAe
            BgkqhkiG9w0BCQEWEWNvbXBhbnlAaW5kaWNlLmdyMIICIjANBgkqhkiG9w0BAQEF
            AAOCAg8AMIICCgKCAgEA49JI4i5PBbl02coZnWaojBa5ToQTZEXthKBMmuSXCpDh
            aNZEBnkbmF0J0xOTZ9BOQlen2TVAdC8inK8DqZC5GH+TuIjVnZf92XqIXxjCP4Lm
            NaAQtolmW5VnYUYuJ4XDunand2cney0YiQ3uDpEWOWDzg3NiMgMDcdvdy7lFFQ9a
            jD1HtX+11EVvyafK5yZD0evwJ83T91seSHgpEWM/5riD5KxsrVW4Jwjz4XDge5GK
            uS7B12I7OpLl/pW2cRUtsQa9T7j3vrr3S2GJU52wypKymT1r2VafxNpXFzSC3n2M
            RVh6ubmyZGpbCux6h/4GmvYcU6nE9jL1g23kU/Vigcn1jyf7m+5oNnmaWw0MgT57
            /QbSf+RnLn/TN+y+Isdm+gGydedLKvZ01IgZe02f/X0cFMjSb+whhoXGPz2bOZtr
            ai2IJmHnLzbVHrz4CnCzbMws6fJhJJC88DNvLd548v6foGI2ZjizLEdBYlJEi03e
            aiVCf0I6J8hUyhXCiLHTBL/kYg0PbUaMlRJE2fnyKYDBiQa6Iin7HbpccSi3834h
            jvpe4XyZYp6HEH6uBccydQov54LquhjA9XJJKAr/419p1S/ycxFJtTIMCdZHs/6/
            Tc3AEw9qho4bqeNrzon7Ooq2LY05AkfI8J95u/eoRtVSF5JEQj+t+21jPrv0W9cC
            AwEAAaNTMFEwHQYDVR0OBBYEFMRInYEFqNlxmNv1iKHJCwD2Er+IMB8GA1UdIwQY
            MBaAFMRInYEFqNlxmNv1iKHJCwD2Er+IMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZI
            hvcNAQELBQADggIBAHK/PhggI3N0I/AJaM8dJAyDgYXTw8GC2u1p/mr7bJWfYVhT
            sLkUnal2AKxtbqtczqtGx/syLZaiGOKotUxfPqSz36xFpj/NYk1oRdGahkMKNDtl
            CtlIxBELdhMKwhahKLQGGybwbxy33YH9oYHQODCQIbWiJuojZaGknAfOq4JGb/bb
            YGtHfgBMho4cMFwCCtnRZiUf98LTOk7SQLCjbDrS5uAGIegkVv4Wms0xLEmfwMz3
            6AUOYio+/uSI/ncdxslMK2OdWxNr+7iHVm0RXsCvVTmQlZDpWCWPQuwKkYEsODLf
            k3XVpVtynyaQwZWbkpr63kZFKqxl8PcGemRjOI7trBRir7h8AN1GWNthnxs7fbRt
            RXt89F/sw51/BPR5VAbE0ONp6p+QvlTvMK99D8W94rblZBbOeab/q6rbo7YOxyTw
            GecMkTnl+aGnk1ggFW/HA/gnavbisLNlzWDW6gzhfKL2IhzV7CUNih16ypgbFq8Y
            rVfWdOS3+Eot4U18MIsQzoq2bTKgSim5xKqLGMq9oSHh5soeTc6RKnAzHIbqmbZN
            OqSZHKXUVXn1025Y+t5izNd/gt23ZzNc3nTf0A7FgVhsVbROEMfSecK4rLVO5IK2
            7Aj8HTznsehUEIWv/giczL5nbz/iN4R6NJ+S4bJTcFMMfoapYM00pj/fjRc+
            -----END CERTIFICATE-----
            ";
        private const string RSAPublicKey =
            @"-----BEGIN PUBLIC KEY-----
            MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA49JI4i5PBbl02coZnWao
            jBa5ToQTZEXthKBMmuSXCpDhaNZEBnkbmF0J0xOTZ9BOQlen2TVAdC8inK8DqZC5
            GH+TuIjVnZf92XqIXxjCP4LmNaAQtolmW5VnYUYuJ4XDunand2cney0YiQ3uDpEW
            OWDzg3NiMgMDcdvdy7lFFQ9ajD1HtX+11EVvyafK5yZD0evwJ83T91seSHgpEWM/
            5riD5KxsrVW4Jwjz4XDge5GKuS7B12I7OpLl/pW2cRUtsQa9T7j3vrr3S2GJU52w
            ypKymT1r2VafxNpXFzSC3n2MRVh6ubmyZGpbCux6h/4GmvYcU6nE9jL1g23kU/Vi
            gcn1jyf7m+5oNnmaWw0MgT57/QbSf+RnLn/TN+y+Isdm+gGydedLKvZ01IgZe02f
            /X0cFMjSb+whhoXGPz2bOZtrai2IJmHnLzbVHrz4CnCzbMws6fJhJJC88DNvLd54
            8v6foGI2ZjizLEdBYlJEi03eaiVCf0I6J8hUyhXCiLHTBL/kYg0PbUaMlRJE2fny
            KYDBiQa6Iin7HbpccSi3834hjvpe4XyZYp6HEH6uBccydQov54LquhjA9XJJKAr/
            419p1S/ycxFJtTIMCdZHs/6/Tc3AEw9qho4bqeNrzon7Ooq2LY05AkfI8J95u/eo
            RtVSF5JEQj+t+21jPrv0W9cCAwEAAQ==
            -----END PUBLIC KEY-----
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
        public async Task<string> Can_Register_New_Device_Using_Biometric() {
            var accessToken = await LoginWithPasswordGrant(userName: "alice", password: "alice");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
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
            var challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
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
            challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
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
                    { "public_key", CertificatePublicKey },
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

        private Task<string> InitiateDeviceRegistrationUsingBiometric(string accessToken, string codeVerifier, string deviceId) => InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "fingerprint");

        private Task<string> InitiateDeviceRegistrationUsingPin(string accessToken, string codeVerifier, string deviceId) => InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "pin");

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

        private Task<HttpResponseMessage> CompleteDeviceRegistrationUsingBiometric(string accessToken, string codeVerifier, string deviceId, string challenge) => CompleteDeviceRegistration(accessToken, codeVerifier, deviceId, challenge, "fingerprint");

        private Task<HttpResponseMessage> CompleteDeviceRegistrationUsingPin(string accessToken, string codeVerifier, string deviceId, string challenge) => CompleteDeviceRegistration(accessToken, codeVerifier, deviceId, challenge, "pin");

        private async Task<HttpResponseMessage> CompleteDeviceRegistration(string accessToken, string codeVerifier, string deviceId, string challenge, string mode) {
            var x509SigningCredentials = GetX509SigningCredentials();
            var signature = SignMessage(challenge, x509SigningCredentials);
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
                data.Add("public_key", RSAPublicKey);
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
            var certificate = X509Certificate2.CreateFromPem(CertificatePublicKey, PrivateKey);
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

        private static string SignMessage(byte[] message, X509SigningCredentials x509SigningCredentials) {
            using var key = x509SigningCredentials.Certificate.GetRSAPrivateKey();
            var signedMessage = Convert.ToBase64String(key?.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1) ?? Array.Empty<byte>());
            return signedMessage;
        }

        private static string SignMessage(string message, X509SigningCredentials x509SigningCredentials) {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            return SignMessage(messageBytes, x509SigningCredentials);
        }

        private static string SignMessage(string message) {
            // Create a UnicodeEncoder to convert between byte array and string.
            var byteConverter = new ASCIIEncoding();
            // Create byte arrays to hold original, encrypted, and decrypted data.
            var originalData = byteConverter.GetBytes(message);
            byte[] signedData;
            // Create a new instance of the RSACryptoServiceProvider class and automatically create a new key-pair.
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider()) {
                rsaCryptoServiceProvider.ImportRSAPrivateKey(Convert.FromBase64String(PrivateKey.Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty).Replace("-----END RSA PRIVATE KEY-----", string.Empty)), out _);
                // Export the key information to an RSAParameters object. You must pass true to export the private key for signing. However, you do not need to export the private key for verification.
                var rsaParameters = rsaCryptoServiceProvider.ExportParameters(true);
                // Hash and sign the data.
                signedData = HashAndSignBytes(originalData, rsaParameters, HashAlgorithmName.SHA256);
                return Convert.ToBase64String(signedData);
            }
        }

        private static byte[] HashAndSignBytes(byte[] dataToSign, RSAParameters rsaParameters, HashAlgorithmName hashAlgorithm) {
            // Create a new instance of RSACryptoServiceProvider using the key from RSAParameters.  
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider()) {
                try {
                    rsaCryptoServiceProvider.ImportParameters(rsaParameters);
                    // Hash and sign the data. Pass a new instance of SHA1CryptoServiceProvider to specify the use of SHA1 for hashing.
                    return rsaCryptoServiceProvider.SignData(dataToSign, hashAlgorithm, RSASignaturePadding.Pkcs1);
                } catch (CryptographicException) {
                    return null;
                } finally {
                    // Set the key container to be cleared when RSA is garbage collected.
                    rsaCryptoServiceProvider.PersistKeyInCsp = false;
                }
            }
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

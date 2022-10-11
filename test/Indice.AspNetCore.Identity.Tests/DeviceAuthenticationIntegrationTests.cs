using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Tests.Models;
using Indice.Configuration;
using Indice.Security;
using Indice.Serialization;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace Indice.AspNetCore.Identity.Tests
{
    public class DeviceAuthenticationIntegrationTests
    {
        #region Keys
        // https://www.scottbrady91.com/OpenSSL/Creating-RSA-Keys-using-OpenSSL
        private const string PRIVATE_KEY =
            @"-----BEGIN RSA PRIVATE KEY-----
            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
            -----END RSA PRIVATE KEY-----
            ";
        private const string CERTIFICATE_PUBLIC_KEY =
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
        private const string RSA_PUBLIC_KEY =
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
        private const string BASE_URL = "https://server";
        private const string CLIENT_ID = "ppk-client";
        private const string CLIENT_SECRET = "JUEKX2XugFv5XrX3";
        private const string DEVICE_PIN = "4412";
        private const string DEVICE_REGISTRATION_INITIATION_URL = $"{BASE_URL}/my/devices/register/init";
        private const string DEVICE_REGISTRATION_COMPLETE_URL = $"{BASE_URL}/my/devices/register/complete";
        private const string DEVICE_AUTHORIZATION_URL = $"{BASE_URL}/my/devices/connect/authorize";
        private const string IDENTITY_DATABASE_NAME = "IdentityDb";
        // Private fields
        private readonly HttpClient _httpClient;
        private readonly ITestOutputHelper _output;
        private IServiceProvider _serviceProvider;

        public DeviceAuthenticationIntegrationTests(ITestOutputHelper output) {
            _output = output;
            var builder = new WebHostBuilder();
            builder.ConfigureAppConfiguration(builder => {
                builder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("IdentityOptions:User:Devices:DefaultAllowedRegisteredDevices", "20"),
                    new KeyValuePair<string, string>("IdentityOptions:User:Devices:MaxAllowedRegisteredDevices", "40"),
                    new KeyValuePair<string, string>("IdentityOptions:User:Devices:RequireCredentialsOnAccountChange", "true")
                });
            });
            builder.ConfigureServices(services => {
                services.AddSingleton<ITotpService, MockTotpService>();
                services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(builder => builder.UseInMemoryDatabase(IDENTITY_DATABASE_NAME));
                services.AddIdentity<User, Role>()
                        .AddUserManager<ExtendedUserManager<User>>()
                        .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                        .AddExtendedSignInManager()
                        .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>();
                services.AddIdentityServer(options => options.EmitStaticAudienceClaim = true)
                        .AddInMemoryIdentityResources(GetIdentityResources())
                        .AddInMemoryApiScopes(GetApiScopes())
                        .AddInMemoryApiResources(GetApiResources())
                        .AddInMemoryClients(GetClients())
                        .AddAspNetIdentity<User>()
                        .AddInMemoryPersistedGrants()
                        .AddExtendedResourceOwnerPasswordValidator()
                        .AddDeviceAuthentication(options => options.AddUserDeviceStoreEntityFrameworkCore())
                        .AddDeveloperSigningCredential(persistKey: false);
                _serviceProvider = services.BuildServiceProvider();
            });
            builder.Configure(app => {
                app.UseIdentityServer();
            });
            var server = new TestServer(builder);
            var handler = server.CreateHandler();
            _httpClient = new HttpClient(handler) {
                BaseAddress = new Uri(BASE_URL)
            };
        }

        #region Facts
        [Fact]
        public async Task Can_Register_New_Device_Using_Biometric() {
            var deviceId = Guid.NewGuid().ToString();
            var response = await Register_Device_Using_Biometric(deviceId);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
            }
            var responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.True(response.IsSuccessStatusCode);
            Assert.IsType<Guid>(responseDto.RegistrationId);
        }

        [Fact]
        public async Task<TrustedDeviceCompleteRegistrationResultDto> Can_Register_Device_Using_Pin_When_Already_Supports_Fingerprint() {
            var accessToken = await LoginWithPasswordGrant(userName: "company@indice.gr", password: "123abc!");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
            }
            var responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.True(response.IsSuccessStatusCode);
            Assert.IsType<Guid>(responseDto.RegistrationId);
            codeVerifier = GenerateCodeVerifier();
            challenge = await InitiateDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId);
            response = await CompleteDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId, challenge);
            responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
            }
            responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.True(response.IsSuccessStatusCode);
            Assert.IsType<Guid>(responseDto.RegistrationId);
            responseDto.DeviceId = deviceId;
            return responseDto;
        }

        [Fact]
        public async Task<string> Can_Register_New_Device_Using_Pin() {
            var accessToken = await LoginWithPasswordGrant(userName: "company@indice.gr", password: "123abc!");
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
        public async Task<TrustedDeviceCompleteRegistrationResultDto> Can_Register_Device_Using_Fingerprint_When_Already_Supports_Pin() {
            var accessToken = await LoginWithPasswordGrant(userName: "company@indice.gr", password: "123abc!");
            var codeVerifier = GenerateCodeVerifier();
            var deviceId = Guid.NewGuid().ToString();
            var challenge = await InitiateDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingPin(accessToken, codeVerifier, deviceId, challenge);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
            }
            var responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.True(response.IsSuccessStatusCode);
            Assert.IsType<Guid>(responseDto.RegistrationId);
            var pinRegistrationId = responseDto.RegistrationId;
            codeVerifier = GenerateCodeVerifier();
            challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
            responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
            }
            responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.True(response.IsSuccessStatusCode);
            Assert.IsType<Guid>(responseDto.RegistrationId);
            var fingerprintRegistrationId = responseDto.RegistrationId;
            Assert.Equal(pinRegistrationId, fingerprintRegistrationId);
            responseDto.DeviceId = deviceId;
            return responseDto;
        }

        [Fact]
        public async Task Can_Authorize_Existing_Device_Using_Fingerprint() {
            var registrationResult = await Can_Register_Device_Using_Fingerprint_When_Already_Supports_Pin();
            var codeVerifier = GenerateCodeVerifier();
            var challenge = await InitiateDeviceAuthorizationUsingFingerprint(codeVerifier, registrationResult.RegistrationId);
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var x509SigningCredentials = GetX509SigningCredentials();
            var signature = SignMessage(challenge, x509SigningCredentials);
            var tokenResponse = await _httpClient.RequestTokenAsync(new TokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = CLIENT_ID,
                ClientSecret = CLIENT_SECRET,
                GrantType = CustomGrantTypes.DeviceAuthentication,
                Parameters = {
                    { "code", challenge },
                    { "code_signature", signature },
                    { "code_verifier", codeVerifier },
                    { "registration_id", registrationResult.RegistrationId.ToString() },
                    { "public_key", CERTIFICATE_PUBLIC_KEY },
                    { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
                }
            });
            Assert.False(tokenResponse.IsError);
        }

        [Fact]
        public async Task Can_Authorize_Existing_Device_Using_Pin() {
            var registrationResult = await Can_Register_Device_Using_Pin_When_Already_Supports_Fingerprint();
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var tokenResponse = await _httpClient.RequestTokenAsync(new TokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = CLIENT_ID,
                ClientSecret = CLIENT_SECRET,
                GrantType = CustomGrantTypes.DeviceAuthentication,
                Parameters = {
                    { "registration_id", registrationResult.RegistrationId.ToString() },
                    { "pin", DEVICE_PIN },
                    { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
                }
            });
            Assert.False(tokenResponse.IsError);
        }

        [Fact(Skip = "Needs configuration change")]
        public async Task Register_More_Devices_Than_Allowed_Fails() {
            var hasAnyError = false;
            foreach (var item in Enumerable.Range(0, 5)) {
                var deviceId = Guid.NewGuid().ToString();
                var response = await Register_Device_Using_Biometric(deviceId);
                if (!response.IsSuccessStatusCode) {
                    hasAnyError = true;
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseJson);
                    var validation = JsonSerializer.Deserialize<ValidationProblemDetails>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings());
                    Assert.Collection(validation.Errors.Keys, errorCode => errorCode.Contains("MaxNumberOfDevices"));
                    break;
                }
            }
            if (!hasAnyError) {
                Assert.Fail("We should have a validation error by the end of the loop.");
            }
        }

        [Fact]
        public async Task Changing_Password_Blocks_Device() {
            var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
            var user = new User {
                CreateDate = DateTimeOffset.UtcNow,
                Email = "g.manoltzas@indice.gr",
                EmailConfirmed = true,
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "69XXXXXXXX",
                PhoneNumberConfirmed = true,
                UserName = "g.manoltzas@indice.gr"
            };
            // 1. Create a new user.
            var result = await userManager.CreateAsync(user, password: "123abc!", validatePassword: false);
            if (!result.Succeeded) {
                Assert.Fail("User could not be created.");
            }
            // 2. Register a new device using biometric login.
            var deviceId = Guid.NewGuid().ToString();
            var response = await Register_Device_Using_Biometric(deviceId, userName: "g.manoltzas@indice.gr");
            if (!response.IsSuccessStatusCode) {
                Assert.Fail("Device could not be created.");
            }
            // 3. Change username. 
            result = await userManager.SetUserNameAsync(user, "g.manoltzas_new@indice.gr");
            if (!result.Succeeded) {
                Assert.Fail("Failed to set new username.");
            }
            var device = await userManager.GetDeviceByIdAsync(user, deviceId);
            if (device is null) {
                Assert.Fail("User device could not be found.");
            }
            // 4. At that point all devices should require username and password in the next login.
            Assert.True(device.RequiresPassword);
            var responseJson = await response.Content.ReadAsStringAsync();
            var responseDto = JsonSerializer.Deserialize<TrustedDeviceCompleteRegistrationResultDto>(responseJson);
            Assert.IsType<Guid>(responseDto.RegistrationId);

            async Task<TokenResponse> LoginWithFingerprint(Guid registrationId) {
                var codeVerifier = GenerateCodeVerifier();
                var challenge = await InitiateDeviceAuthorizationUsingFingerprint(codeVerifier, registrationId);
                var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
                var x509SigningCredentials = GetX509SigningCredentials();
                var signature = SignMessage(challenge, x509SigningCredentials);
                return await _httpClient.RequestTokenAsync(new TokenRequest {
                    Address = discoveryDocument.TokenEndpoint,
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET,
                    GrantType = CustomGrantTypes.DeviceAuthentication,
                    Parameters = {
                        { "code", challenge },
                        { "code_signature", signature },
                        { "code_verifier", codeVerifier },
                        { "registration_id", registrationId.ToString() },
                        { "public_key", CERTIFICATE_PUBLIC_KEY },
                        { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
                    }
                });
            }

            // 5. Login with fingerprint. This is expected to fail.
            var tokenResponse = await LoginWithFingerprint(responseDto.RegistrationId);
            Assert.True(tokenResponse.IsError);
            Assert.True(tokenResponse.ErrorDescription == "requires_password");
            // 6. Now we login with username and password using the resource owner password grant. This should make the device usable again.
            await LoginWithPasswordGrant("g.manoltzas_new@indice.gr", "123abc!", deviceId);
            // 7. Login again with fingerprint. This is expected to succeed.
            tokenResponse = await LoginWithFingerprint(responseDto.RegistrationId);
            Assert.False(tokenResponse.IsError);
        }
        #endregion

        #region Helper Methods
        private async Task<HttpResponseMessage> Register_Device_Using_Biometric(string deviceId, string userName = "company@indice.gr") {
            var accessToken = await LoginWithPasswordGrant(userName, password: "123abc!");
            var codeVerifier = GenerateCodeVerifier();
            var challenge = await InitiateDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId);
            var response = await CompleteDeviceRegistrationUsingBiometric(accessToken, codeVerifier, deviceId, challenge);
            return response;
        }

        private async Task<string> InitiateDeviceAuthorizationUsingFingerprint(string codeVerifier, Guid registrationId) {
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var data = new Dictionary<string, string> {
                { "client_id", CLIENT_ID },
                { "code_challenge", codeChallenge },
                { "registration_id", registrationId.ToString() },
                { "scope", $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1" }
            };
            var form = new FormUrlEncodedContent(data);
            var response = await _httpClient.PostAsync(DEVICE_AUTHORIZATION_URL, form);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                _output.WriteLine(responseJson);
                throw new HttpRequestException();
            }
            var result = JsonSerializer.Deserialize<TrustedDeviceAuthorizationResultDto>(responseJson);
            return result.Challenge;
        }

        private async Task<string> LoginWithPasswordGrant(string userName, string password, string deviceId = null) {
            var discoveryDocument = await _httpClient.GetDiscoveryDocumentAsync();
            var request = new PasswordTokenRequest {
                Address = discoveryDocument.TokenEndpoint,
                ClientId = CLIENT_ID,
                ClientSecret = CLIENT_SECRET,
                Scope = $"{IdentityServerConstants.StandardScopes.OpenId} {IdentityServerConstants.StandardScopes.Phone} scope1",
                UserName = userName,
                Password = password
            };
            if (!string.IsNullOrEmpty(deviceId)) {
                request.Parameters.Add("device_id", deviceId);
            }
            var tokenResponse = await _httpClient.RequestPasswordTokenAsync(request);
            return tokenResponse.AccessToken;
        }

        private Task<string> InitiateDeviceRegistrationUsingBiometric(string accessToken, string codeVerifier, string deviceId) => InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "fingerprint");

        private Task<string> InitiateDeviceRegistrationUsingPin(string accessToken, string codeVerifier, string deviceId) => InitiateDeviceRegistration(accessToken, codeVerifier, deviceId, "pin");

        private async Task<string> InitiateDeviceRegistration(string accessToken, string codeVerifier, string deviceId, string mode) {
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var data = new Dictionary<string, string> {
                { "code_challenge", codeChallenge },
                { "device_id", deviceId },
                { "mode", mode },
                { "channel", TotpDeliveryChannel.Viber.ToString() }
            };
            var form = new FormUrlEncodedContent(data);
            _httpClient.SetBearerToken(accessToken);
            var response = await _httpClient.PostAsync(DEVICE_REGISTRATION_INITIATION_URL, form);
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
                data.Add("public_key", RSA_PUBLIC_KEY);
            }
            if (mode == "pin") {
                data.Add("pin", DEVICE_PIN);
            }
            var form = new FormUrlEncodedContent(data);
            _httpClient.SetBearerToken(accessToken);
            return await _httpClient.PostAsync(DEVICE_REGISTRATION_COMPLETE_URL, form);
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
            var certificate = X509Certificate2.CreateFromPem(CERTIFICATE_PUBLIC_KEY, PRIVATE_KEY);
            var signingCredentials = new X509SigningCredentials(certificate, SecurityAlgorithms.RsaSha256Signature);
            return signingCredentials;
        }

        private static SigningCredentials GetSigningCredentials() {
            var privateKey = Convert.FromBase64String(PRIVATE_KEY);
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
                rsaCryptoServiceProvider.ImportRSAPrivateKey(Convert.FromBase64String(PRIVATE_KEY.Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty).Replace("-----END RSA PRIVATE KEY-----", string.Empty)), out _);
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
                ClientId = CLIENT_ID,
                ClientName = "Public/Private key client",
                AccessTokenType = AccessTokenType.Jwt,
                AllowAccessTokensViaBrowser = false,
                AllowedGrantTypes = {
                    CustomGrantTypes.DeviceAuthentication,
                    GrantType.ClientCredentials,
                    GrantType.ResourceOwnerPassword
                },
                ClientSecrets = {
                    new Secret(CLIENT_SECRET.ToSha256())
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
                    new ClientClaim(BasicClaimTypes.TrustedDevice, "true", ClaimValueTypes.Boolean),
                    new ClientClaim(BasicClaimTypes.MobileClient, "true", ClaimValueTypes.Boolean)
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
        #endregion

        #region Helper Classes
        public class TrustedDeviceCompleteRegistrationResultDto
        {
            public string DeviceId { get; set; }
            [JsonPropertyName("registrationId")]
            public Guid RegistrationId { get; set; }
        }
        #endregion
    }
}

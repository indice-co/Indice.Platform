using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Indice.Extensions;
using Indice.Features.GovGr.Models;
using Indice.Features.GovGr.Proxies.Gsis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.GovGr.Tests;

public class ClientCreationTests : IDisposable
{
    public ClientCreationTests() {
        var inMemorySettings = new Dictionary<string, string> {
            ["GovGr:Kyc:Environment"] = "Mock",
            ["GovGr:Kyc:ClientId"] = "ebanking",
            ["GovGr:Kyc:ClientSecret"] = "secret",
            ["GovGr:Wallet:Sandbox"] = "true",
            ["GovGr:Wallet:Token"] = "XX",
            ["GovGr:BusinessRegistry:BaseAddress"] = "https://www1.gsis.gr:443/webtax2/wsgsis/RgWsPublic/RgWsPublicPort",
            ["GovGr:BusinessRegistry:Username"] = "USERxxxxxxxxx",
            ["GovGr:BusinessRegistry:Password"] = "afmxxxxxxxxx",
            ["GovGr:BusinessRegistry:CallersFiscalCode"] = "xxxxxxxxxx",
            ["TestGreekIdentityNumber"] = "000",
            ["TestOTP"] = "000000",
            //...populate as needed for the test
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .AddUserSecrets<ClientCreationTests>(optional: true)
            .Build();
        var collection = new ServiceCollection()
            .AddSingleton(configuration)
            .AddGovGrClient();
        ServiceProvider = collection.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    [Fact]
    public async Task CreateMockClient() {
        /// TODO do something meaningful
        var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
        var data = await govGR.Kyc(clientId: null, clientSecret: null, redirectUri: null, environment: "Mock").GetDataAsync("123");
        Assert.NotNull(data);
    }

    [Fact]
    public void CreateKycClient() {
        /// TODO do something meaningful
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
        //var data = await govGR.Kyc().GetDataAsync(configuration["GovGr:Kyc:Code"]);
        var data = govGR.Kyc().GetAvailableScopes();
        Assert.NotNull(data);
    }

    [Fact(Skip = "Needs Creds")]
    public async Task CreateBusinessRegistryClient() {
        var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
        var data = await govGR.BusinessRegistry().GetBusinessRegistry("xxxxxxxxx");
        Assert.NotNull(data);
    }

    [Fact(Skip = "Fix it please.")]
    public async Task CreateDocumentsClient() {
        /// TODO do something meaningful

        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
        var data = await govGR.Documents(token: configuration["GovGr:Wallet:Token"], serviceName: "GOV-WALLET-PRESENT-ID-DEMO").PostAsync(new() {
            Document = new() {
                Template = new() {
                    DigestSha256 = string.Empty
                },
                Statements = new() {
                    IdNumber = configuration["TestGreekIdentityNumber"]
                },
                Attachments = new()
            }
        });
        Assert.NotNull(data);
    }

    [Fact(Skip = "Fix it please.")]
    public async Task CreateWalletClient() {
        /// TODO do something meaningful

        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
        var reference = await govGR.Wallet().RequestIdentificationAsync(configuration["TestGreekIdentityNumber"]);
        var data = await govGR.Wallet().GetIdentificationAsync(reference.DeclarationId, "000000");

        Assert.NotNull(data);
    }

    [Fact]
    async public Task ValidateSignature() {
        var responseJsonString = File.ReadAllText("kyc-test-response.json");
        // Deserialize KycResponse
        var encodedResponse = JsonSerializer.Deserialize<KycHttpResponse>(responseJsonString);
        // Decode Protected
        var protectedJsonString = encodedResponse.Protected.Base64UrlSafeDecode();
        // Deserialize decoded Protected
        var @protected = JsonSerializer.Deserialize<Protected>(protectedJsonString);

        // Get Public Certificate
        var client = new HttpClient();
        var httpResponse = await client.GetAsync(@protected.X5u);
        var certificatePemString = await httpResponse.Content.ReadAsStringAsync();

        // convert certificate string into X509 certificate
        // https://stackoverflow.com/a/65352811/19162333
        certificatePemString = certificatePemString.Replace("-----BEGIN CERTIFICATE-----", null).Replace("-----END CERTIFICATE-----", null);
        var certificateByteArray = Convert.FromBase64String(certificatePemString);
        var certificate = new X509Certificate2(certificateByteArray);
        // use X509 certificate to create a signatureProvider
        var securityKey = new X509SecurityKey(certificate);
        var cryptoProviderFactory = securityKey.CryptoProviderFactory;
        var signatureProvider = cryptoProviderFactory.CreateForVerifying(securityKey, "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");

        // Signature is BASE64URL Encoded!
        var signatureByteArray = Base64UrlEncoder.DecodeBytes(encodedResponse.Signature);
        // notice that we "JWT-concat" Protected & Payload!
        var token = $"{encodedResponse.Protected}.{encodedResponse.Payload}";
        var encodedByteArray = Encoding.UTF8.GetBytes(token);

        // Is signature valid?
        var isValid = signatureProvider.Verify(encodedByteArray, signatureByteArray);
        // cleanup...
        cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);

        Assert.True(isValid);
    }

    public void Dispose() {
        ServiceProvider.Dispose();
    }
}
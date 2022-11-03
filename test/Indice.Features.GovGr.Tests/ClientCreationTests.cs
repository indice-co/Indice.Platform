using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.GovGr.Tests
{
    public class ClientCreationTests : IDisposable
    {
        public ClientCreationTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["GovGr:Kyc:Environment"] = "Mock",
                ["GovGr:Kyc:ClientId"] = "ebanking",
                ["GovGr:Kyc:ClientSecret"] = "secret",
                ["GovGr:Wallet:Sandbox"] = "true",
                ["GovGr:Wallet:Token"] = "XX",
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
        public async Task CreateKycClient() {
            /// TODO do something meaningful
            var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
            var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
            //var data = await govGR.Kyc().GetDataAsync(configuration["GovGr:Kyc:Code"]);
            var data = govGR.Kyc().GetAvailableScopes();
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

        public void Dispose() {
            ServiceProvider.Dispose();
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.GovGr.Tests
{
    public class ClientCreationTests : IDisposable
    {
        public ClientCreationTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["GovGr:Kyc:Environment"] = "Mock",
                ["GovGr:Kyc:Credentials:Name"] = "Web",
                ["GovGr:Kyc:Credentials:ClientId"] = "ebanking",
                ["GovGr:Kyc:Credentials:ClientSecret"] = "secret",
                ["GovGr:Wallet:Sandbox"] = "true",
                ["GovGr:Wallet:Token"] = "XX",
                ["TestGreekIdentityNumber"] = "000",
                ["TestOTP"] = "000000",
                //...populate as needed for the test
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .AddUserSecrets<ClientCreationTests>(optional:true)
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
            var data = await govGR.Kyc(clientCredentials: null, environment:"Mock").GetDataAsync("123");
            Assert.NotNull(data);
        }

        [Fact]
        public async Task CreateDocumentsClient() {
            /// TODO do something meaningful

            var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
            var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
            var data = await govGR.Documents(serviceName: "GOV-WALLET-PRESENT-ID-DEMO", token: configuration["GovGr:Wallet:Token"]).PostAsync(new() {
                Document = new() {
                    Template = new() {
                        DigestSha256 = string.Empty
                    },
                    Statements = new () {
                        IdNumber = configuration["TestGreekIdentityNumber"]
                    },
                    Attachments = new()
                }
            });
            Assert.NotNull(data);
        }

        [Fact]
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
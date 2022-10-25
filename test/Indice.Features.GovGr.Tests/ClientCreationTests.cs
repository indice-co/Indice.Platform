using System.Drawing;
using Indice.Features.GovGr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.GovGr.Tests
{
    public class ClientCreationTests : IDisposable
    {
        public ClientCreationTests() {
            var inMemorySettings = new Dictionary<string, string> {
                ["GovGr:UseMockServices"] = "true",
                ["GovGr:Clients:0:Name"] = "Web",
                ["GovGr:Clients:0:ClientId"] = "ebanking",
                ["GovGr:Clients:0:ClientSecret"] = "secret",
                //...populate as needed for the test
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var collection = new ServiceCollection()
                .AddSingleton(configuration)
                .AddGovGrClient();
            ServiceProvider = collection.BuildServiceProvider();
        }

        public ServiceProvider ServiceProvider { get; }

        [Fact]
        public void CreateMockClient() {
            /// TODO do something meaningful
            /// 
            var govGR = ServiceProvider.GetRequiredService<GovGrClient>();
            var data = govGR?.Kyc("Web").GetData("123");
            Assert.NotNull(data);
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}
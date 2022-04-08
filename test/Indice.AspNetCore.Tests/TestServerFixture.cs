using Indice.AspNetCore.Views;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Tests
{
    public class TestServerFixture : WebApplicationFactory<TestStartup>
    {
        public TService GetRequiredService<TService>() {
            if (Server == null) {
                CreateDefaultClient();
            }
            return Server.Host.Services.GetRequiredService<TService>();
        }

        protected override IWebHostBuilder CreateWebHostBuilder() {
            var hostBuilder = new WebHostBuilder();
            hostBuilder.ConfigureAppConfiguration((context, builder) => {
                context.HostingEnvironment.ApplicationName = typeof(ViewsMarker).Assembly.GetName().Name;
            });
            return hostBuilder.UseStartup<TestStartup>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.UseSolutionRelativeContentRoot("test/Indice.AspNetCore.Views");
        }
    }
}

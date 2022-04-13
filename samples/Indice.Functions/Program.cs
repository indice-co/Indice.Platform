using System;
using System.Threading.Tasks;
using Indice.Features.Messages.Worker.Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Indice.Functions
{
    public class Program
    {
        public async static Task Main(params string[] args) {
            // Create the host builder instance.
            var host = new HostBuilder();
            // Configure function host.
            host.ConfigureAppConfiguration((context, config) => {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureFunctionsWorkerDefaults(builder => { })
            .ConfigureServices(services => { })
            .ConfigureMessages((configuration, options) => {
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
                options.DatabaseSchema = "cmp";
                options.UseEventDispatcherAzure()
                       //.UsePushNotificationServiceAzure((serviceProvider, options) => { })
                       .UseIdentityContactResolver(resolverOptions => {
                           resolverOptions.BaseAddress = new Uri(configuration["IdentityServer:BaseAddress"]);
                           resolverOptions.ClientId = configuration["IdentityServer:ClientId"];
                           resolverOptions.ClientSecret = configuration["IdentityServer:ClientSecret"];
                       });
            })
            .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"));
            // Build host and run.
            await host.Build().RunAsync();
        }
    }
}

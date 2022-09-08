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
            var hostBuilder = new HostBuilder();
            // Configure function host.
            hostBuilder.ConfigureAppConfiguration((context, config) => {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureFunctionsWorkerDefaults(
                builder => {
                    builder.UseMiddleware<MessagesMiddleware>();
                }, 
                options => {
                    options.InputConverters.RegisterAt<MessagesInputConverter>(0);
                }
            )
            .ConfigureServices((context, services) => { })
            .ConfigureMessageFunctions((configuration, hostingEnvironment, options) => {
                options.DatabaseSchema = "cmp";
                options.ConfigureDbContext = (serviceProvider, builder) => {
                    if (hostingEnvironment.IsDevelopment()) {
                        builder.EnableDetailedErrors();
                        builder.EnableSensitiveDataLogging();
                    }
                    //var tenant = serviceProvider.GetRequiredService<ITenantAccessor<ExtendedTenant>>().Tenant;
                    builder.UseSqlServer(/*tenant?.ConnectionString ?? */configuration.GetConnectionString("MultitenantApiDb"));
                };
                options.UseEventDispatcherAzure()
                       .UsePushNotificationServiceAzure()
                       .UseEmailServiceSparkpost(configuration)
                       .UseSmsServiceYubotoOmni(configuration)
                       .UseIdentityContactResolver(resolverOptions => {
                           resolverOptions.BaseAddress = new Uri(configuration["IdentityServer:BaseAddress"]);
                           resolverOptions.ClientId = configuration["IdentityServer:ClientId"];
                           resolverOptions.ClientSecret = configuration["IdentityServer:ClientSecret"];
                       });
            })
            .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"));
            // Build host and run.
            var host = hostBuilder.Build();
            await host.RunAsync();
        }
    }
}

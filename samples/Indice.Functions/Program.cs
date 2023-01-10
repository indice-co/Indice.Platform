using Indice.Features.Multitenancy.Core;
using Indice.Sample.Common.Data;
using Indice.Sample.Common.Models;
using Indice.Sample.Common.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Functions
{
    public class Program
    {
        public const bool MULTITENANCY = false;

        public async static Task Main(params string[] args) {
            // Create the host builder instance.
            var hostBuilder = new HostBuilder();
            // Configure function host.
            hostBuilder.ConfigureAppConfiguration((context, config) => {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) => {
                services.AddDbContext<SaasDbContext>(builder => {
                    if (context.HostingEnvironment.IsDevelopment()) {
                        builder.EnableDetailedErrors();
                        builder.EnableSensitiveDataLogging();
                    }
                    builder.UseSqlServer(context.Configuration.GetConnectionString("SaasDb"));
                });
                services.AddMultiTenancy<ExtendedTenant>()
                        .FromQueueTriggerPayload()
                        .WithStore<SaasTenantStore>();
            })
            .ConfigureFunctionsWorkerDefaults((context, builder) => {
                builder.UseFunctionContextAccessor();
                builder.UseMultiTenancy<ExtendedTenant>();
            })
            .ConfigureMessageFunctions((configuration, hostingEnvironment, options) => {
                options.DatabaseSchema = "msg";
                options.ConfigureDbContext = (serviceProvider, builder) => {
                    if (hostingEnvironment.IsDevelopment()) {
                        builder.EnableDetailedErrors();
                        builder.EnableSensitiveDataLogging();
                    }
                    var tenant = serviceProvider.GetRequiredService<ITenantAccessor<ExtendedTenant>>().Tenant;
                    builder.UseSqlServer(tenant?.ConnectionString ?? configuration.GetConnectionString("MessagesDb"));
                };
                options.UseEventDispatcherAzure((serviceProvider, eventDispatcherOptions) => {
                    var tenant = serviceProvider.GetService<ITenantAccessor<ExtendedTenant>>().Tenant;
                    eventDispatcherOptions.TenantIdSelector = () => tenant?.Identifier;
                });
                options.UsePushNotificationServiceAzure((serviceProvider, pushNotificationsOptions) => {
                    var tenant = serviceProvider.GetService<ITenantAccessor<ExtendedTenant>>().Tenant;
                    pushNotificationsOptions.ConnectionString = tenant?.PushNotificationConnectionString;
                });
                options.UseFilesAzure();
                options.UseEmailServiceSparkpost(configuration);
                options.UseSmsServiceYubotoOmni(configuration);
                options.UseIdentityContactResolver(resolverOptions => {
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

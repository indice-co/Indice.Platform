using System;
using Indice.Extensions.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace Indice.Identity
{
    /// <summary>
    /// The bootstrap class of the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The entry point of the web application.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static void Main(string[] args) {
            var host = CreateWebHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Class {nameof(Program)} is initializing the web host.");
            host.Run();
        }

        /// <summary>
        /// Buildes the web host.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .ConfigureLogging(builder => {
                       // https://docs.microsoft.com/en-us/azure/azure-monitor/app/ilogger
                       var serviceProvider = builder.Services.BuildServiceProvider();
                       var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                       builder.AddApplicationInsights(configuration.GetInstrumentationKey());
                       builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Trace);
                   })
                   .UseEFConfiguration(reloadInterval: TimeSpan.FromHours(1), connectionStringName: "IdentityDb");
    }
}

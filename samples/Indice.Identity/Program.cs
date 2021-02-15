using System;
using Indice.Extensions.Configuration.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Indice.Identity
{
    /// <summary>
    /// The bootstrap class of the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The entry point of the web application.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static int Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            try {
                Log.Information("Starting web host.");
                CreateHostBuilder(args).Build().Run();
                return 0;
            } catch (Exception exception) {
                Log.Fatal(exception, "Host terminated unexpectedly.");
                return 1;
            } finally {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Buildes the web host.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static IHostBuilder CreateHostBuilder(string[] args) {
            AppDomain.CurrentDomain.ProcessExit += (sender, @event) => Log.CloseAndFlush();
            return Host.CreateDefaultBuilder(args)
                       .ConfigureWebHostDefaults(webHostBuilder => {
                           webHostBuilder.UseStartup<Startup>();
                       })
                       .UseEFConfiguration(reloadInterval: TimeSpan.FromHours(1), connectionStringName: "IdentityDb")
                       .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
        }
    }
}

using System;
using Indice.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        /// <summary>
        /// Builds the host.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHostBuilder => {
                    webHostBuilder.UseStartup<Startup>();
                })
                .UseEntityConfiguration(options => {
                    var configuration = options.Configuration;
                    options.ReloadOnInterval = TimeSpan.FromMinutes(1);
                    options.ReloadOnDatabaseChange = true;
                    options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
                });
    }
}

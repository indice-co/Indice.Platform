using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

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
                .UseDatabaseConfiguration<ExtendedIdentityDbContext<User, Role>>((options, configuration) => {
                    options.ReloadOnInterval = TimeSpan.FromMinutes(1);
                    options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
                });
    }
}

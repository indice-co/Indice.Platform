using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

        /// <summary>
        /// Buildes the web host.
        /// </summary>
        /// <param name="args">Optional command line arguments.</param>
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseApplicationInsights()
                   .UseStartup<Startup>();
    }
}

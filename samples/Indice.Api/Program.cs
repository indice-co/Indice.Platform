using System;
using Indice.Api.Data;
using Indice.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Indice.Api
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                })
                .UseDatabaseConfiguration<ApiDbContext>(options => {
                    var configuration = options.Configuration;
                    options.ReloadOnInterval = TimeSpan.FromMinutes(1);
                    options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("SettingsDb"));
                });
    }
}

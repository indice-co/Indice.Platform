using Indice.Api.Data;
using Indice.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Indice.Api;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.UseStartup<Startup>();
            })
            .UseDatabaseConfiguration<ApiDbContext>((options, configuration) => {
                options.ReloadOnInterval = TimeSpan.FromMinutes(1);
                options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("SettingsDb"));
            });
}

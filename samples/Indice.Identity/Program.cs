using System;
using Indice.Extensions.Configuration.EFCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

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
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false).Build();
            var columnOptions = new ColumnOptions();
            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.LogEvent.DataLength = 2048;
            columnOptions.PrimaryKey = columnOptions.TimeStamp;
            columnOptions.TimeStamp.NonClusteredIndex = true;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(
                    connectionString: configuration.GetConnectionString("IdentityDb"),
                    tableName: "Logs",
                    columnOptions: columnOptions,
                    autoCreateSqlTable: true
                 )
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
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHostBuilder => {
                    webHostBuilder.UseStartup<Startup>();
                })
                .UseEFConfiguration(reloadInterval: TimeSpan.FromHours(1), connectionStringName: "IdentityDb")
                .UseSerilog();
    }
}

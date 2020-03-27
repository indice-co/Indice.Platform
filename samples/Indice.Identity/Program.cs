using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using Indice.Extensions.Configuration.EFCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;
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
            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Add(StandardColumn.LogEvent);
            columnOptions.AdditionalColumns = new Collection<SqlColumn> {
                new SqlColumn {
                    AllowNull = true,
                    ColumnName = "UserId",
                    DataLength = 64,
                    DataType = SqlDbType.NVarChar,
                    NonClusteredIndex = true
                }
            };
            columnOptions.PrimaryKey = columnOptions.Id;
            columnOptions.TimeStamp.NonClusteredIndex = true;
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
               .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
               .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
               .Enrich.WithMachineName()
               .Enrich.WithThreadId()
               .Enrich.FromLogContext()
               .WriteTo.Console()
               .WriteTo.MSSqlServer(
                   connectionString: configuration.GetConnectionString("IdentityDb"),
                   schemaName: "log",
                   tableName: "Logs",
                   columnOptions: columnOptions,
                   autoCreateSqlTable: true,
                   restrictedToMinimumLevel: LogEventLevel.Information,
                   batchPostingLimit: 50,
                   period: TimeSpan.FromMinutes(1)
                )
               .CreateLogger();
#if DEBUG
            SelfLog.Enable(message => Debug.WriteLine(message));
#endif
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
                       .UseSerilog();
        }
    }
}

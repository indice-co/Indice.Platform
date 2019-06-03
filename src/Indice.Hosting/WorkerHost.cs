using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{

    /// <summary>
    /// Worker configuration extenstions for the <see cref="IHostBuilder"/>
    /// </summary>
    public static class WorkerHost
    {

        //https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-2.2
        /// <summary>
        /// Creates a <see cref="IHostBuilder"/> with all defaults in configuration plus the Windows service spesifics for base directory and eventlog registration.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateWindowsServiceBuilder(string[] args) {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            args = args.Where(arg => arg != "--console").ToArray();
            if (isService) {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }
            return CreateDefaultBuilder(args)
                 .ConfigureLogging((hostingContext, logging) => {
                     logging.AddEventLog();
                 })
                .ConfigureServices((hostContext, services) => {
                    services.AddSingleton(new WorkerHostOptions {
                        RunAsService = isService
                    });
                });
        }

        /// <summary>
        /// Creates an <see cref="IHostBuilder"/> with the default configuration.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateDefaultBuilder(string[] args) {
            return new HostBuilder()
                .ConfigureHostConfiguration(configHost => {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                    configHost.AddEnvironmentVariables(prefix: "NETCORE_");
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) => {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                       $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                       optional: true);
                    configApp.AddEnvironmentVariables(prefix: "NETCORE_");
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) => {
                    services.AddLogging();
                })
                .ConfigureLogging((hostContext, builder) => {
                    builder.AddConsole();
                    builder.AddDebug();
                });
        }

        internal static void RunAsServiceInternal(this IHost host) {
            var hostService = new GenericServiceHost(host);
            ServiceBase.Run(hostService);
        }

        /// <summary>
        /// Runs in the context of a windows service.
        /// </summary>
        /// <param name="host"></param>
        public static void RunAsService(this IHost host) {
            var shouldRunAsService = host.Services.GetService<WorkerHostOptions>()?.RunAsService;
            if (shouldRunAsService == true) {
                host.RunAsServiceInternal();
            } else {
                host.Run();
            }
        }
    }
}

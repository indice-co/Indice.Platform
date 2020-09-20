using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.BackgroundTasks.AspNetCore
{
    /// <summary>
    /// Bootstraps and manages the long-running process of monitoring and handling background tasks.
    /// </summary>
    /// <remarks>https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=visual-studio#queued-background-tasks</remarks>
    public class BackgroundTaskHostedService : BackgroundService
    {
        private readonly BackgroundTaskServer _backgroundTaskServer;
        private readonly ILogger<BackgroundTaskHostedService> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="BackgroundTaskHostedService"/>.
        /// </summary>
        /// <param name="backgroundTaskServer">A process that runs continuously and processes work items that need to be processed asynchronously.</param>
        /// <param name="logger">A generic interface for logging.</param>
        /// <inheritdoc/>
        public BackgroundTaskHostedService(BackgroundTaskServer backgroundTaskServer, ILogger<BackgroundTaskHostedService> logger) {
            _backgroundTaskServer = backgroundTaskServer ?? throw new ArgumentNullException(nameof(backgroundTaskServer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation($"{nameof(BackgroundTaskHostedService)} is now running.");
            return _backgroundTaskServer.Start();
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken stoppingToken) {
            _logger.LogInformation($"{nameof(BackgroundTaskHostedService)} is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}

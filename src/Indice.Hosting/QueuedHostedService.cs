using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public QueuedHostedService(ILogger<QueuedHostedService> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public override Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is running.");
            return base.StartAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            return base.StopAsync(cancellationToken);
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IJobFactory _jobFactory;

        /// <summary>
        /// Creates a new instance of <see cref="QueuedHostedService"/>.
        /// </summary>
        /// <param name="schedulerFactory">rovides a mechanism for obtaining client-usable handles to <see cref="IScheduler"/> instances.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="jobFactory"></param>
        public QueuedHostedService(ISchedulerFactory schedulerFactory, ILogger<QueuedHostedService> logger, IJobFactory jobFactory) {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        }

        /// <summary>
        /// The Quartz scheduler.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is running.");
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            await Scheduler.Start(cancellationToken);
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            await Scheduler?.Shutdown(cancellationToken);
        }
    }
}

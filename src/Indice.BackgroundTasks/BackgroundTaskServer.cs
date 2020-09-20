using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Indice.BackgroundTasks.Abstractions;
using Microsoft.Extensions.Logging;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// A process that runs continuously and processes work items that need to be processed asynchronously.
    /// </summary>
    public class BackgroundTaskServer
    {
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly ILogger<BackgroundTaskServer> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="BackgroundTaskServer"/>.
        /// </summary>
        /// <param name="backgroundTaskQueue">Manages the scheduling of background tasks.</param>
        /// <param name="logger">A generic interface for logging.</param>
        public BackgroundTaskServer(IBackgroundTaskQueue backgroundTaskQueue, ILogger<BackgroundTaskServer> logger) {
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private CancellationTokenSource CancellationTokenSource { get; set; }
        private Task BackgroundTaskRunnerThread { get; set; }

        /// <summary>
        /// Starts the <see cref="BackgroundTaskServer"/> that checks for pending work items to process.
        /// </summary>
        public async Task Start() {
            StartInternal();
            await Task.WhenAll(BackgroundTaskRunnerThread);
        }

        /// <summary>
        /// Stops the <see cref="BackgroundTaskServer"/>.
        /// </summary>
        public void Stop() { }

        private void StartInternal() {
            if (BackgroundTaskRunnerThread != null) {
                throw new Exception($"The {nameof(BackgroundTaskServer)} has been started multiple times.");
            }
            CancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = CancellationTokenSource.Token;
            BackgroundTaskRunnerThread = Task.Run(async () => {
                while (!cancellationToken.IsCancellationRequested) {
                    await ExecuteBackgroundTask(cancellationToken);
                }
            }, cancellationToken);
        }

        private async Task ExecuteBackgroundTask(CancellationToken cancellationToken) {
            var task = await _backgroundTaskQueue.Dequeue(cancellationToken);
            try {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                await task(cancellationToken);
                stopwatch.Stop();
                _logger.LogInformation("Background task executed successfully in {0}ms.", stopwatch.ElapsedMilliseconds);
            } catch (Exception exception) {
                _logger.LogError("An error occured while executing a background task. Exception is: {0}", exception.ToString());
                throw;
            }
        }
    }
}

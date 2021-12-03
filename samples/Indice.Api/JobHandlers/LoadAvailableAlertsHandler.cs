using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.Api.JobHandlers
{
    public class LoadAvailableAlertsHandler
    {
        private readonly ILogger<LoadAvailableAlertsHandler> _logger;
        private readonly IMessageQueue<SmsDto> _messageQueue;

        public LoadAvailableAlertsHandler(ILogger<LoadAvailableAlertsHandler> logger, IMessageQueue<SmsDto> messageQueue) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public async Task Process(DemoCounterModel state, ILockManager lockManager, CancellationToken cancellationToken) {
            var @lock = await lockManager.TryAcquireLock("alerts-lock");
            using (var x = @lock.Lock) {
                var timer = new Stopwatch();
                timer.Start();
                var tasksList = new List<SmsDto>();
                var task = new SmsDto(userId: Guid.NewGuid().ToString(), phoneNumber: "6992731575", message: $"Transaction '{Guid.NewGuid()}' took place.");
                for (var i = 0; i < 2; i++) {
                    tasksList.Add(task);
                }
                var waitTime = new Random().Next(15, 20) * 1000;
                await Task.Delay(waitTime, cancellationToken);
                await _messageQueue.EnqueueRange(tasksList, visibilityWindow: TimeSpan.FromMinutes(1));
                timer.Stop();
                _logger.LogDebug("{HandlerName} took {ExecutionTime}ms to execute.", nameof(LoadAvailableAlertsHandler), timer.ElapsedMilliseconds);
            }
        }
    }

    public class DemoCounterModel
    {
        public int DemoCounter { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class DemoCounterModel
    {
        public int DemoCounter { get; set; }
    }

    public class LoadAvailableAlertsHandler
    {
        private readonly ILogger<LoadAvailableAlertsHandler> _logger;
        private readonly IMessageQueue<SmsDto> _messageQueue;

        public LoadAvailableAlertsHandler(ILogger<LoadAvailableAlertsHandler> logger, IMessageQueue<SmsDto> messageQueue) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public async Task Process(DemoCounterModel state, ILockManager lockManager, CancellationToken cancellationToken) {
            var timer = new Stopwatch();
            timer.Start();
            var tasksList = new List<SmsDto>();
            var task = new SmsDto(userId: Guid.NewGuid().ToString(), phoneNumber: "6992731575", message: $"Transaction {Guid.NewGuid()} has been taken place.");
            for (var i = 0; i < 2; i++) {
                tasksList.Add(task);
            }
            await _messageQueue.EnqueueRange(tasksList);
            var waitTime = new Random().Next(15, 20) * 1000;
            await Task.Delay(waitTime, cancellationToken);
            timer.Stop();
            _logger.LogDebug($"{nameof(LoadAvailableAlertsHandler)} took {timer.ElapsedMilliseconds}ms to execute.");
        }
    }
}
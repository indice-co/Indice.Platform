using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting;
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
        private readonly IMessageQueue<SMSDto> _messageQueue;

        public LoadAvailableAlertsHandler(ILogger<LoadAvailableAlertsHandler> logger, IMessageQueue<SMSDto> messageQueue) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public async Task Process(DemoCounterModel state, ILockManager lockManager, CancellationToken cancellationToken) {
            /*var lockResult = await lockManager.TryAquireLock(nameof(LoadAvailableAlertsHandler));
            if (!lockResult.Ok) {
                return;
            }
            using (lockResult.Lock) {*/
                // 1.Load 10.000 items from source.
                // 2.Find max source id,
                // 3.Bach Enqueue to IMessageQueue.
                // 4.Update as processed where source up until max source id.
                // 5.Save max source id to state as mark.
                state.DemoCounter++;
                if (state.DemoCounter > 100) {
                    return;
                }
                await _messageQueue.EnqueueRange(new List<SMSDto> {
                    new SMSDto(Guid.NewGuid().ToString(), "6992731575", $"Hello there! {state.DemoCounter}"),
                    new SMSDto(Guid.NewGuid().ToString(), "6992731576", $"How are you today? {state.DemoCounter}"),
                    new SMSDto(Guid.NewGuid().ToString(), "6992731577", $"You look nice! {state.DemoCounter}"),
                    new SMSDto(Guid.NewGuid().ToString(), "6992731578", $"Let's go... {state.DemoCounter}"),
                    new SMSDto(Guid.NewGuid().ToString(), "6992731579", $"Hello there again! {state.DemoCounter}")
                });
                _logger.LogInformation("Start: {Id} at {Timestamp} {counter}", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow, state.DemoCounter);
                var waitTime = new Random().Next(5, 10) * 1000;
                _logger.LogInformation("Durat: {Id} Process will last {0}ms", nameof(LoadAvailableAlertsHandler), waitTime);
                await Task.Delay(waitTime);
                _logger.LogInformation("Ended: {Id} at {Timestamp} ", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow);
            /*}*/
        }
    }
}

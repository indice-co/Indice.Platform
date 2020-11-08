using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class LoadAvailableAlertsHandler
    {
        private readonly ILogger<LoadAvailableAlertsHandler> _logger;
        private readonly IMessageQueue<SMSDto> _messageQueue;

        public LoadAvailableAlertsHandler(ILogger<LoadAvailableAlertsHandler> logger, IMessageQueue<SMSDto> messageQueue) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public async Task Process(IDictionary<string, object> state, CancellationToken cancellationToken) {
            // 1. load 10.000 items from source ()
            // 2. Find max source ID
            // 3. Bach Enqueue to IMessageQueue
            // 4. Update as processed where source up until max source ID.
            // 5. save max source ID to state as MARK
            var count = (int)(state["DemoCounter"] ?? 0) + 1;
            state["DemoCounter"] = count;
            if (count > 100) {
                return;
            }
            await _messageQueue.EnqueueRange(new List<SMSDto> {
                new SMSDto(Guid.NewGuid().ToString(), "6992731575", $"Hello there! {count}"),
                new SMSDto(Guid.NewGuid().ToString(), "6992731576", $"How are you today? {count}"),
                new SMSDto(Guid.NewGuid().ToString(), "6992731577", $"You look nice! {count}"),
                new SMSDto(Guid.NewGuid().ToString(), "6992731578", $"Let's go... {count}"),
                new SMSDto(Guid.NewGuid().ToString(), "6992731579", $"Hello there again! {count}")
            });
            _logger.LogInformation("Start: {Id} at {Timestamp} {counter}", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow, count);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Durat: {Id} Process will last {0}ms", nameof(LoadAvailableAlertsHandler), waitTime);
            await Task.Delay(waitTime);
            _logger.LogInformation("Ended: {Id} at {Timestamp} ", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow);
        }
    }
}
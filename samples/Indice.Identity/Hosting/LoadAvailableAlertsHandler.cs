using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class LoadAvailableAlertsHandler
    {
        private readonly ILogger<LoadAvailableAlertsHandler> _logger;

        public LoadAvailableAlertsHandler(ILogger<LoadAvailableAlertsHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(IDictionary<string, object> state, CancellationToken cancellationToken) {
            // 1. load 10.000 items from source ()
            // 2. Find max source ID
            // 3. Bach Enqueue to IMessageQueue
            // 4. Update as processed where source up until max source ID.
            // 5. save max source ID to state as MARK


            state["DemoCounter"] = (int)(state["DemoCounter"] ?? 0) + 1;
            
            _logger.LogInformation("Start: {Id} at {Timestamp} {counter}", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow, state["DemoCounter"]);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Durat: {Id} Process will last {0}ms", nameof(LoadAvailableAlertsHandler), waitTime);
            await Task.Delay(waitTime);
            _logger.LogInformation("Ended: {Id} at {Timestamp} ", nameof(LoadAvailableAlertsHandler), DateTime.UtcNow);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class TestJobHandler
    {
        private readonly ILogger<TestJobHandler> _logger;

        public TestJobHandler(ILogger<TestJobHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(CancellationToken cancellationToken) {
            _logger.LogInformation("Start: {Id} at {Timestamp}", nameof(TestJobHandler), DateTime.UtcNow);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Durat: {Id} Process will last {0}ms", nameof(TestJobHandler), waitTime);
            await Task.Delay(waitTime);
            _logger.LogInformation("Ended: {Id} at {Timestamp} ", nameof(TestJobHandler), DateTime.UtcNow);
        }
    }
}

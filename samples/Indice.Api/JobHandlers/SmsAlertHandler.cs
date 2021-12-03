using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Api.JobHandlers
{
    public class SmsAlertHandler
    {
        private readonly ILogger<SmsAlertHandler> _logger;

        public SmsAlertHandler(ILogger<SmsAlertHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(SmsDto message) {
            if (message == null) {
                return;
            }
            var timer = new Stopwatch();
            timer.Start();
            var waitTime = new Random().Next(5, 10) * 100;
            await Task.Delay(waitTime);
            _logger.LogDebug("{HandlerName} took {ExecutionTime}ms to execute.", nameof(SmsAlertHandler), timer.ElapsedMilliseconds);
        }
    }
}

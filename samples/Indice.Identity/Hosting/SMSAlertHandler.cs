using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
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
            _logger.LogDebug($"{nameof(SmsAlertHandler)} took {timer.ElapsedMilliseconds}ms to execute.");
        }
    }
}
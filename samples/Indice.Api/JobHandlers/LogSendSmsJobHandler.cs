using System;
using Microsoft.Extensions.Logging;

namespace Indice.Api.JobHandlers
{
    public class LogSendSmsJobHandler
    {
        private readonly ILogger<LogSendSmsJobHandler> _logger;

        public LogSendSmsJobHandler(ILogger<LogSendSmsJobHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(LogSmsDto message) {
            if (message == null) {
                return;
            }
            _logger.LogDebug("An SMS was sent to '{PhoneNumber}'.", message.PhoneNumber);
        }
    }
}

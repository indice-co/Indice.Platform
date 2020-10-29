using System;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class UserMessageJobHandler
    {
        private readonly ILogger<UserMessageJobHandler> _logger;

        public UserMessageJobHandler(ILogger<UserMessageJobHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(UserMessage userMessage) {
            if (userMessage == null) {
                return;
            }
            _logger.LogInformation("Processing work item: {userMessage} at {Timestamp} with message {message}", userMessage, DateTime.UtcNow, userMessage.Message);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Process will last {0}ms", waitTime);
            await Task.Delay(waitTime);
        }
    }
}

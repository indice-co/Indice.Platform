using System;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class UserMessageHandler : IWorkItemHandler<UserMessage>
    {
        private readonly ILogger<UserMessageHandler> _logger;

        public UserMessageHandler(ILogger<UserMessageHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Process(UserMessage workItem) {
            if (workItem == null) {
                return;
            }
            _logger.LogInformation("Processing work item: {WorkItem} at {Timestamp} with lease id {LeaseId}", workItem, DateTime.UtcNow, workItem.LeaseId);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Process will last {0}ms", waitTime);
            await Task.Delay(waitTime);
        }
    }
}

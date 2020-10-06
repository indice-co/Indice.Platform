using System;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class UserMessageJobHandler : JobHandler
    {
        private readonly ILogger<UserMessageJobHandler> _logger;

        public UserMessageJobHandler(UserMessage userMessage, ILogger<UserMessageJobHandler> logger) : base(userMessage) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async override Task Process() {
            if (WorkItem == null) {
                return;
            }
            var workItem = (UserMessage)WorkItem;
            _logger.LogInformation("Processing work item: {WorkItem} at {Timestamp} with lease id {LeaseId}", workItem, DateTime.UtcNow, workItem.LeaseId);
            var waitTime = new Random().Next(5, 10) * 1000;
            _logger.LogInformation("Process will last {0}ms", waitTime);
            await Task.Delay(waitTime);
        }
    }
}

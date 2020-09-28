using System;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Hosting
{
    public class UserMessageHandler : WorkItemHandler<UserMessage>
    {
        private ILogger<UserMessageHandler> _logger;

        public UserMessageHandler(ILogger<UserMessageHandler> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task Process(UserMessage workItem) {
            _logger.LogDebug("Received work item: {WorkItem}", workItem);
            return Task.CompletedTask;
        }
    }
}

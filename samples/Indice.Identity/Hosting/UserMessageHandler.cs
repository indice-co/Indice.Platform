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

        public Task Process(UserMessage workItem) {
            _logger.LogInformation("Processing work item: {WorkItem} at {Timestamp}", workItem, DateTime.UtcNow);
            return Task.CompletedTask;
        }
    }
}

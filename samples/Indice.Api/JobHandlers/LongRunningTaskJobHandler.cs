using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Api.JobHandlers
{
    public class LongRunningTaskJobHandler
    {
        private readonly ILogger<LongRunningTaskJobHandler> _logger;

        public LongRunningTaskJobHandler(ILogger<LongRunningTaskJobHandler> logger) {
            _logger = logger;
        }

        public async Task Process(CancellationToken cancellationToken) {
            _logger.LogInformation("I am a useless long running task.");
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            _logger.LogInformation("Useless long running task completed.");
        }
    }
}

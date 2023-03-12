using Microsoft.Extensions.Logging;

namespace Indice.WorkerHost.JobHandlers;

public class SendSmsJobHandler
{
    private readonly ILogger<LoadAvailableAlertsJobHandler> _logger;

    public SendSmsJobHandler(ILogger<LoadAvailableAlertsJobHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Process(SmsDto message) {
        if (message == null) {
            return;
        }
        _logger.LogInformation("Started processing alert: {Alert}", message.ToString());
        var waitTime = new Random().Next(5, 10) * 100;
        await Task.Delay(waitTime);
    }
}

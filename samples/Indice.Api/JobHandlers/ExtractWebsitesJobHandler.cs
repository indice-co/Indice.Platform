using Indice.Hosting.Services;

namespace Indice.Api.JobHandlers;

public class ExtractWebsitesJobHandler
{
    private readonly ILogger<ExtractWebsitesJobHandler> _logger;
    private readonly IMessageQueue<DownloadWebsiteCommand> _messageQueue;

    public ExtractWebsitesJobHandler(
        ILogger<ExtractWebsitesJobHandler> logger,
        IMessageQueue<DownloadWebsiteCommand> messageQueue
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
    }

    public async Task Process(ExtractWebsitesCommand command) {
        var counter = 0;
        var limit = command.Limit ?? int.MaxValue;
        _logger.LogDebug("{CommandName} received to read file from path '{FilePath}'.", nameof(ExtractWebsitesCommand), command.FilePath);
        await foreach (var line in File.ReadLinesAsync(command.FilePath)) {
            if (counter == limit) {
                break;
            }
            _logger.LogDebug("Sending command to read content for website '{Url}'", line);
            await _messageQueue.Enqueue(new DownloadWebsiteCommand(line));
            counter++;
        }
    }
}

public record ExtractWebsitesCommand(string FilePath, int? Limit);

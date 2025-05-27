using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Open.ChannelExtensions;

namespace Indice.Features.Messages.Core.Services;
internal class UserActionHandler(
    UserActionQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<UserActionHandler> logger) : BackgroundService
{
    /// <summary>Batch size for dequeuing events from the queue.</summary>
    public int DequeueBatchSize { get; set; } = 10;
    /// <summary>Timeout for dequeuing events from the queue in milliseconds.</summary>
    public long DequeueTimeoutInMilliseconds { get; set; } = 1000;

    ///<inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var events = queue.Reader
                 .Pipe(LastEvent => LastEvent, cancellationToken: stoppingToken)
                 .Filter(logEntry => logEntry is not null)
                 .Batch(DequeueBatchSize)
                 .WithTimeout(DequeueTimeoutInMilliseconds)
                 .ReadAllAsync(stoppingToken);

        await foreach (var lastActivityBatch in events) {
            foreach (var activity in lastActivityBatch) {
                if (activity.Action == "MarkAllAsRead")
                    await HandleMarkAsReadAction(activity, stoppingToken);
                logger.LogInformation("Processing UserEvent: {UserEvent}", activity);
            }
        }
    }

    /// <summary>Upserts a batch of campaign events into the database.</summary>
    private async Task HandleMarkAsReadAction(UserEvent userEvent, CancellationToken stoppingToken) {

        using var scope = scopeFactory.CreateScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        var options = new ListOptions() { Page = 1, Size = 100 };
        var filter = new MessagesFilter() { IsRead = true };
        while (true) {
            var messages = await messageService.GetList(userEvent.UserCode, ListOptions.Create(options, filter));
            if (messages == null || messages.Count == 0) return; // No messages to process.
            foreach (var messageId in messages.Items.Select(x => x.Id)) {
                try {
                    await messageService.MarkAsRead(messageId, userEvent.UserCode);
                    await Task.Delay(100, stoppingToken);
                } catch (BusinessException be) {
                    logger.LogWarning(be, "Could not mark message {Messageid} as read.", messageId);
                } catch (DbUpdateException dbEx) {
                    logger.LogError(dbEx, "Database update failed while marking {Messageid} as read.", messageId);
                } catch (OperationCanceledException ocEx) when (stoppingToken.IsCancellationRequested) {
                    logger.LogWarning(ocEx, "Operation was canceled while marking {Messageid} as read.", messageId);
                } catch (Exception ex) {
                    logger.LogError(ex, "An unexpected error occurred while marking {Messageid} as read.", messageId);
                }
            }
        }
    }
}
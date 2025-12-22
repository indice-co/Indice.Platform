using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Azure;

/// <summary>
/// Provides scheduled trigger methods for recurring background jobs using cron expressions.
/// </summary>
public class CronTriggers
{
    private MessageJobHandlerFactory CleanUpJobHandlerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CronTriggers"/> class.
    /// </summary>
    /// <param name="cleanUpJobHandlerFactory"></param>
    /// <param name="eventDispatcherFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CronTriggers(
        MessageJobHandlerFactory cleanUpJobHandlerFactory,
        IEventDispatcherFactory eventDispatcherFactory
    ) {
        CleanUpJobHandlerFactory = cleanUpJobHandlerFactory ?? throw new ArgumentNullException(nameof(cleanUpJobHandlerFactory));
    }


    /// <summary>
    /// Deletes the data of campaigns that do not have inbox.
    /// </summary>
    /// <param name="myTimer"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    [Function("DatabaseCleanUp")]
    public async Task RunDatabaseCleanUp([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, DatabaseCleanUpHandler handler) {
        var payload = new DatabaseCleanUpTimerEvent();
        await CleanUpJobHandlerFactory.CreateFor<DatabaseCleanUpTimerEvent>().Process(payload);
    }

}
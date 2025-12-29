using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Azure.Functions.Worker;

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
    /// <exception cref="ArgumentNullException"></exception>
    public CronTriggers(
        MessageJobHandlerFactory cleanUpJobHandlerFactory) {
        CleanUpJobHandlerFactory = cleanUpJobHandlerFactory ?? throw new ArgumentNullException(nameof(cleanUpJobHandlerFactory));
    }


    /// <summary>
    /// Performs database cleanup for all campaigns based on their respective retention policies.
    /// </summary>
    /// <param name="myTimer">The timer trigger that schedules the database cleanup job.</param>
    /// <param name="handler">The handler responsible for executing the database cleanup logic.</param>
    /// <returns>A task that represents the asynchronous database cleanup operation.</returns>
    [Function("DatabaseCleanUp")]
    public async Task RunDatabaseCleanUp([TimerTrigger("%MessageJobsOptions:DatabaseCleanUpCronExpression%")] TimerInfo myTimer, DatabaseCleanUpHandler handler) {
        var payload = new DatabaseCleanUpTimerEvent();
        await CleanUpJobHandlerFactory.CreateFor<DatabaseCleanUpTimerEvent>().Process(payload);
    }

}
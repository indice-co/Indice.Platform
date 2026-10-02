using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Azure.Functions.Worker;

namespace Indice.Features.Messages.Worker.Azure;

/// <summary>
/// Provides scheduled trigger methods for recurring background jobs using cron expressions.
/// </summary>
public class CronTriggers
{
    internal const string StatsVerifyFunctionName = "MessagingStatsVerify";
    internal const string StatsVerifyCronExpressionSetting = "MessageJobsOptions:StatsVerifyCronExpression";

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
    /// <returns>A task that represents the asynchronous database cleanup operation.</returns>
    [Function("MessagingDatabaseCleanUp")]
    public async Task RunMessagingDatabaseCleanUp([TimerTrigger("%MessageJobsOptions:DatabaseCleanUpCronExpression%")] TimerInfo myTimer) {
        var payload = new MessagingDatabaseCleanUpTimerEvent();
        await CleanUpJobHandlerFactory.CreateFor<MessagingDatabaseCleanUpTimerEvent>().Process(payload);
    }

    /// <summary>
    /// Recounts the successful sends of the last complete days and corrects the send statistics.
    /// </summary>
    /// <param name="myTimer">The timer trigger that schedules the verification job.</param>
    /// <returns>A task that represents the asynchronous verification operation.</returns>
    [Function(StatsVerifyFunctionName)]
    public async Task RunMessagingStatsVerify([TimerTrigger("%" + StatsVerifyCronExpressionSetting + "%")] TimerInfo myTimer) {
        var payload = new MessageStatsVerifyTimerEvent();
        await CleanUpJobHandlerFactory.CreateFor<MessageStatsVerifyTimerEvent>().Process(payload);
    }

}
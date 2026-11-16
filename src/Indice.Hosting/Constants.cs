namespace Indice.Hosting;

/// <summary>Naming conventions for Hosting feature</summary>
public class HostingConventions
{
    /// <summary>Poison Queue Name suffix convention.</summary>
    /// <remarks>Used when a message is moved to the poison queue then the original queue name is suffixed with this value.</remarks>
    public const string PoisonSuffix = "-poison";
    /// <summary>Prefix of the internal queues used to request an immediate run of a scheduled task.</summary>
    /// <remarks>A run is requested by inserting a message in the <c>{prefix}{taskId}</c> queue, where the task id is the Quartz job key (<c>{group}.{name}</c>).</remarks>
    public const string RunNowQueuePrefix = "$run-now:";

    /// <summary>Gets the name of the internal queue used to request an immediate run of the scheduled task with the given id.</summary>
    /// <param name="taskId">The scheduled task id, i.e. the Quartz job key (<c>{group}.{name}</c>).</param>
    public static string GetRunNowQueueName(string taskId) => RunNowQueuePrefix + taskId;
}

internal class JobGroups
{
    public const string InternalJobsGroup = nameof(InternalJobsGroup);
}

internal class TriggerNames 
{
    public const string DequeueJobTrigger = nameof(DequeueJobTrigger);
}

internal class JobDataKeys 
{
    public const string QueueName = nameof(QueueName);
    public const string JobHandlerType = nameof(JobHandlerType);
    public const string PollingInterval = nameof(PollingInterval);
    public const string MaxPollingInterval = nameof(MaxPollingInterval);
    public const string MaxRetryCount = nameof(MaxRetryCount);
    public const string CleanUpBatchSize = nameof(CleanUpBatchSize);
    public const string BackoffIndex = nameof(BackoffIndex);
    public const string Singleton = nameof(Singleton);
    public const string RunNow = nameof(RunNow);
}
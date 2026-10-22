namespace Indice.Hosting;

/// <summary>Naming conventions for Hosting feature</summary>
public class HostingConventions
{
    /// <summary>Poison Queue Name suffix convention.</summary>
    /// <remarks>Used when a message is moved to the poison queue then the original queue name is suffixed with this value.</remarks>
    public const string PoisonSuffix = "-poison";
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
}
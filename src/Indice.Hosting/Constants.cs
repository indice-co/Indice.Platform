namespace Indice.Hosting
{
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
        public const string CleanUpBatchSize = nameof(CleanUpBatchSize);
        public const string BackoffIndex = nameof(BackoffIndex);
    }
}

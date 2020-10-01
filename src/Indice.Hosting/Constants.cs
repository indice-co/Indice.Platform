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
    }
}

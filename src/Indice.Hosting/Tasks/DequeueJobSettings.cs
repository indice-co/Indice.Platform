using System;

namespace Indice.Hosting
{
    /// <summary>
    /// Contains metadata about a job to execute.
    /// </summary>
    internal class DequeueJobSettings
    {

        /// <summary>
        /// Creates a new instance of <see cref="DequeueJobSettings"/>.
        /// </summary>
        /// <param name="jobHandlerType">The CLR type of the job's handler.</param>
        /// <param name="workItemType">The CLR type of the job's work item.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="pollingInterval">The time interval between two attempts to dequeue new items. In milliseconds</param>
        /// <param name="backoffThreshold">The maximum time interval between two attempts to dequeue new items. In milliseconds</param>
        /// <param name="instanceCount">Number of concurrent instances</param>
        public DequeueJobSettings(Type jobHandlerType, Type workItemType, string jobName, int pollingInterval, int backoffThreshold, int instanceCount) {
            JobHandlerType = jobHandlerType;
            WorkItemType = workItemType;
            Name = jobName;
            PollingInterval = pollingInterval;
            InstanceCount = instanceCount;
        }

        /// <summary>
        /// The CLR type of the job's handler.
        /// </summary>
        public Type JobHandlerType { get; }
        /// <summary>
        /// The CLR type of the job's work item.
        /// </summary>
        public Type WorkItemType { get; }
        /// <summary>
        /// The name of the job.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The time interval between two attempts to dequeue new items. Mesured in millisecconds
        /// </summary>
        public int PollingInterval { get; set; } = 300;
        /// <summary>
        /// The maximum time interval between two attempts to dequeue new items. Mesured in millisecconds
        /// </summary>
        public int MaxPollingInterval { get; set; } = 5000;
        /// <summary>
        /// The concurrent instance count.
        /// </summary>
        public int InstanceCount { get; set; } = 1;
    }
}

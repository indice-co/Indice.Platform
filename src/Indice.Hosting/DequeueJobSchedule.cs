using System;

namespace Indice.Hosting
{
    /// <summary>
    /// Contains metadata about a job to execute.
    /// </summary>
    internal class DequeueJobSchedule
    {
        /// <summary>
        /// Creates a new instance of <see cref="DequeueJobSchedule"/>.
        /// </summary>
        /// <param name="jobHandlerType">The CLR type of the job's handler.</param>
        /// <param name="workItemType">The CLR type of the job's work item.</param>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="pollingIntervalInSeconds">The time interval between two attempts to dequeue new items.</param>
        public DequeueJobSchedule(Type jobHandlerType, Type workItemType, string jobName, int pollingIntervalInSeconds) {
            JobHandlerType = jobHandlerType;
            WorkItemType = workItemType;
            Name = jobName;
            PollingIntervalInSeconds = pollingIntervalInSeconds;
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
        /// The time interval between two attempts to dequeue new items.
        /// </summary>
        public int PollingIntervalInSeconds { get; set; }
    }
}

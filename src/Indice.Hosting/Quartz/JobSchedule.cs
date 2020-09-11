using System;

namespace Indice.Hosting.Quartz
{
    /// <summary>
    /// Job schedule. This is what to execute and when.
    /// </summary>
    public class JobSchedule
    {
        /// <summary>
        /// Constructs the schedule given a job type and the cron expression
        /// </summary>
        /// <param name="jobType"></param>
        /// <param name="cronExpression"></param>
        /// <param name="group">Job group</param>
        /// <param name="description">The job description</param>
        public JobSchedule(Type jobType, string cronExpression, string group, string description) {
            JobType = jobType;
            CronExpression = cronExpression;
            Group = group;
            Description = description;
        }

        /// <summary>
        /// The CLR type for the job.
        /// </summary>
        public Type JobType { get; }
        /// <summary>
        /// The cron expression.
        /// </summary>
        public string CronExpression { get; }
        /// <summary>
        /// The job description.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// The job group.
        /// </summary>
        public string Group { get; }
    }
}

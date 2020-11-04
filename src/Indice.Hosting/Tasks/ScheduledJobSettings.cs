using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting
{
    /// <summary>
    /// Job schedule. This is what to execute and when.
    /// </summary>
    public class ScheduledJobSettings
    {
        /// <summary>
        /// Constructs the schedule given a job type and the cron expression
        /// </summary>
        /// <param name="jobHandlerType"></param>
        /// <param name="cronExpression"></param>
        /// <param name="group">Job group</param>
        /// <param name="description">The job description</param>
        public ScheduledJobSettings(Type jobHandlerType, string cronExpression, string group, string description) {
            JobHandlerType = jobHandlerType;
            CronExpression = cronExpression;
            Group = group;
            Description = description;
        }

        /// <summary>
        /// The clr type for the job.
        /// </summary>
        public Type JobHandlerType { get; }

        /// <summary>
        /// The cron expression
        /// </summary>
        public string CronExpression { get; }

        /// <summary>
        /// The job description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The job group
        /// </summary>
        public string Group { get; }
    }
}

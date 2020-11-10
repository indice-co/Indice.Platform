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
        /// <param name="jobDataStateType"></param>
        /// <param name="cronExpression"></param>
        /// <param name="name">Job name</param>
        /// <param name="group">Job group</param>
        /// <param name="description">The job description</param>
        public ScheduledJobSettings(Type jobHandlerType, Type jobDataStateType, string cronExpression, string name, string group, string description) {
            JobHandlerType = jobHandlerType;
            JobStateType = jobDataStateType;
            CronExpression = cronExpression;
            Name = name ?? jobHandlerType.Name;
            Group = group;
            Description = description;
        }

        /// <summary>
        /// The clr type for the job.
        /// </summary>
        public Type JobHandlerType { get; }

        /// <summary>
        /// The clr type for the job.
        /// </summary>
        public Type JobStateType { get; }

        /// <summary>
        /// The cron expression
        /// </summary>
        public string CronExpression { get; }

        /// <summary>
        /// the job Name
        /// </summary>
        public string Name { get; }

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

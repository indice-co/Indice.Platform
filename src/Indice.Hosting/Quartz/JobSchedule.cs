using System;
using System.Collections.Generic;
using System.Text;

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
        public JobSchedule(Type jobType, string cronExpression) {
            JobType = jobType;
            CronExpression = cronExpression;
        }

        /// <summary>
        /// The clr type for the job.
        /// </summary>
        public Type JobType { get; }

        /// <summary>
        /// The cron expression
        /// </summary>
        public string CronExpression { get; }
    }
}

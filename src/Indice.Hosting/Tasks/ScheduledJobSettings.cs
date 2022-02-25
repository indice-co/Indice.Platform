using System;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// Job schedule settings. Describes what to execute and when.
    /// </summary>
    public class ScheduledJobSettings
    {
        /// <summary>
        /// Constructs the schedule given a job type and the cron expression.
        /// </summary>
        /// <param name="jobHandlerType">The CLR type for the job.</param>
        /// <param name="jobDataStateType">The type of the job state object.</param>
        /// <param name="cronExpression">The cron expression.</param>
        /// <param name="name">The job name.</param>
        /// <param name="group">The job group.</param>
        /// <param name="description">The job description.</param>
        /// <param name="singleton">The job alowes only one instance to run at any given time.</param>
        public ScheduledJobSettings(Type jobHandlerType, Type jobDataStateType, string cronExpression, string name, string group, string description, bool singleton = false) {
            JobHandlerType = jobHandlerType;
            JobStateType = jobDataStateType;
            CronExpression = cronExpression;
            Name = name ?? jobHandlerType.Name;
            Group = group;
            Description = description;
            Singleton = singleton;
        }

        /// <summary>
        /// The CLR type for the job.
        /// </summary>
        public Type JobHandlerType { get; }
        /// <summary>
        /// The type of the job state object.
        /// </summary>
        public Type JobStateType { get; }
        /// <summary>
        /// The cron expression.
        /// </summary>
        public string CronExpression { get; }
        /// <summary>
        /// The job name.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The job description.
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// The job group.
        /// </summary>
        public string Group { get; }
        /// <summary>
        /// The job alowes only one instance to run at any given time.
        /// </summary>
        public bool Singleton { get; }
    }
}

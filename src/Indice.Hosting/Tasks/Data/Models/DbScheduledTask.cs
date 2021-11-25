using System;
using System.Text.Json;

namespace Indice.Hosting.Tasks.Data.Models
{
    /// <summary>
    /// Tracks a queue message task
    /// </summary>
    public class DbScheduledTask
    {
        /// <summary>
        /// The id.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The worker id that run the task.
        /// </summary>
        public string WorkerId { get; set; }
        /// <summary>
        /// Task group.
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// Task name.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type name.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTimeOffset LastExecution { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTimeOffset? NextExecution { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public int ExecutionCount { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public ScheduledTaskStatus Status { get; set; }
        /// <summary>
        /// The errors.
        /// </summary>
        public string Errors { get; set; }
        /// <summary>
        /// The payload.
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Generates the DTO for this <see cref="DbScheduledTask"/>.
        /// </summary>
        /// <param name="options">Provides options to be used with <see cref="JsonSerializer"/>.</param>
        public ScheduledTask<TState> ToModel<TState>(JsonSerializerOptions options = null) where TState : class => new() {
            Id = Id,
            Description = Description,
            Group = Group,
            Errors = Errors,
            ExecutionCount = ExecutionCount,
            LastExecution = LastExecution,
            NextExecution = NextExecution,
            Progress = Progress,
            State = JsonSerializer.Deserialize<TState>(Payload, options ?? WorkerJsonOptions.GetDefaultSettings()),
            Status = Status,
            Type = Type,
            WorkerId = WorkerId
        };

        internal DbScheduledTask From<TState>(ScheduledTask<TState> model, JsonSerializerOptions options = null) where TState : class {
            Id = model.Id;
            Description = model.Description;
            Group = model.Group;
            Errors = model.Errors;
            ExecutionCount = model.ExecutionCount;
            LastExecution = model.LastExecution;
            NextExecution = model.NextExecution;
            Progress = model.Progress;
            Payload = JsonSerializer.Serialize(model.State, options ?? WorkerJsonOptions.GetDefaultSettings());
            Status = model.Status;
            Type = model.Type;
            WorkerId = model.WorkerId;
            return this;
        }
    }
}

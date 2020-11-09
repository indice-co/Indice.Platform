using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Tracks a queue message task
    /// </summary>
    public class DbScheduledTask
    {
        /// <summary>
        /// The id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The worker id that run the task
        /// </summary>
        public string WorkerId { get; set; }
        /// <summary>
        /// Task group
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// Task name
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type name
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime Lastxecution { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime? NextExecution { get; set; }
        /// <summary>
        /// The date.
        /// </summary>
        public DateTime ExecutionCount { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public ScheduledTaskStatus Status { get; set; }
        /// <summary>
        /// The errors
        /// </summary>
        public string Errors { get; set; }
        /// <summary>
        /// The payload
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// The status.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Generate the dto for this <see cref="DbScheduledTask"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public ScheduledTask ToModel(JsonSerializerOptions options = null) {
            return new ScheduledTask {
                Id = Id,
                Description = Description,
                Group = Group,
                Errors = Errors,
                ExecutionCount = ExecutionCount,
                Lastxecution = Lastxecution,
                NextExecution = NextExecution,
                Progress = Progress,
                State = JsonSerializer.Deserialize<Dictionary<string, object>>(Payload, options ?? WorkerJsonOptions.GetDefaultSettings()),
                Status = Status,
                Type = Type,
                WorkerId = WorkerId
            };
        }

        /// <summary>
        /// Converts the <see cref="ScheduledTask"/> back to the EF representation
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static DbScheduledTask FromModel<TState>(ScheduledTask<TState> model, JsonSerializerOptions options = null) where TState : class {
            return new DbScheduledTask {
                Id = model.Id,
                Description = model.Description,
                Group = model.Group,
                Errors = model.Errors,
                ExecutionCount = model.ExecutionCount,
                Lastxecution = model.Lastxecution,
                NextExecution = model.NextExecution,
                Progress = model.Progress,
                Payload = JsonSerializer.Serialize(model.State, options ?? WorkerJsonOptions.GetDefaultSettings()),
                Status = model.Status,
                Type = model.Type,
                WorkerId = model.WorkerId
            };
        }
    }
}

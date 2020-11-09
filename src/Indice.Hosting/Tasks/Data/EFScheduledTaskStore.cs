using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Entity framework backed store implementation for <see cref="IScheduledTaskStore"/>
    /// </summary>
    public class EFScheduledTaskStore : IScheduledTaskStore
    {
        private readonly TaskDbContext _DbContext;
        private readonly JsonSerializerOptions _JsonOptions;
        /// <summary>
        /// creates the <see cref="EFScheduledTaskStore"/>
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="workerJsonOptions"></param>
        public EFScheduledTaskStore(TaskDbContext dbContext, WorkerJsonOptions workerJsonOptions) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _JsonOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /// <inheritdoc/>
        public async Task<ScheduledTask> GetById(string taskId) {
            return (await _DbContext.Tasks.AsNoTracking().Where(x => x.Id == taskId).SingleOrDefaultAsync())?.ToModel(_JsonOptions);
        }
        /// <inheritdoc/>
        public async Task Save(ScheduledTask scheduledTask) {
            if (await _DbContext.Tasks.AnyAsync(x => x.Id == scheduledTask.Id)) {
                _DbContext.Tasks.Update(DbScheduledTask.FromModel(scheduledTask));
            } else {
                _DbContext.Tasks.Add(DbScheduledTask.FromModel(scheduledTask));
            }
            await _DbContext.SaveChangesAsync();    
        }
    }
}

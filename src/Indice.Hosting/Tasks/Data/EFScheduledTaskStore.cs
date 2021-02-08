using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// Entity framework backed store implementation for <see cref="IScheduledTaskStore{TState}"/>
    /// </summary>
    public class EFScheduledTaskStore<TState> : IScheduledTaskStore<TState> where TState : class
    {
        private readonly TaskDbContext _DbContext;
        private readonly JsonSerializerOptions _JsonOptions;
        /// <summary>
        /// creates the <see cref="EFScheduledTaskStore{TState}"/>
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="workerJsonOptions"></param>
        public EFScheduledTaskStore(TaskDbContext dbContext, WorkerJsonOptions workerJsonOptions) {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _JsonOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /// <inheritdoc/>
        public async Task<ScheduledTask<TState>> GetById(string taskId) {
            return (await _DbContext.Tasks.AsNoTracking().Where(x => x.Id == taskId).SingleOrDefaultAsync())?.ToModel<TState>(_JsonOptions);
        }

        /// <inheritdoc/>
        public async Task Save(ScheduledTask<TState> scheduledTask) {
            var dbTask = (await _DbContext.Tasks.Where(x => x.Id == scheduledTask.Id).SingleOrDefaultAsync());
            if (dbTask != null) {
                dbTask.From(scheduledTask, _JsonOptions);
                _DbContext.Tasks.Update(dbTask);
            } else {
                _DbContext.Tasks.Add(new DbScheduledTask().From(scheduledTask, _JsonOptions));
            }
            await _DbContext.SaveChangesAsync();    
        }
    }
}

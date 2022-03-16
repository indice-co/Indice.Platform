using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Hosting.Tasks.Implementations
{
    /// <summary>
    /// Entity framework backed store implementation for <see cref="IScheduledTaskStore{TState}"/>.
    /// </summary>
    /// <typeparam name="TState">The type of state object.</typeparam>
    public class RelationalScheduledTaskStore<TState> : IScheduledTaskStore<TState> where TState : class
    {
        private readonly TaskDbContext _dbContext;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        /// <summary>
        /// Creates a new instance of <see cref="RelationalScheduledTaskStore{TState}"/>.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="workerJsonOptions"></param>
        public RelationalScheduledTaskStore(TaskDbContext dbContext, WorkerJsonOptions workerJsonOptions) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _jsonSerializerOptions = workerJsonOptions?.JsonSerializerOptions ?? throw new ArgumentNullException(nameof(workerJsonOptions));
        }

        /// <inheritdoc/>
        public async Task<ScheduledTask<TState>> GetById(string taskId) =>
            (await _dbContext.Tasks.AsNoTracking().Where(x => x.Id == taskId).SingleOrDefaultAsync())?.ToModel<TState>(_jsonSerializerOptions);

        /// <inheritdoc/>
        public async Task Save(ScheduledTask<TState> scheduledTask) {
            var dbTask = await _dbContext.Tasks.Where(x => x.Id == scheduledTask.Id).SingleOrDefaultAsync();
            if (dbTask != null) {
                dbTask.From(scheduledTask, _jsonSerializerOptions);
                _dbContext.Tasks.Update(dbTask);
            } else {
                _dbContext.Tasks.Add(new DbScheduledTask().From(scheduledTask, _jsonSerializerOptions));
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}

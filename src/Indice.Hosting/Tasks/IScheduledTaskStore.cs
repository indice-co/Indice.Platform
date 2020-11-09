using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public interface IScheduledTaskStore
    {
        Task Save(ScheduledTask scheduledTask);
        Task<ScheduledTask> GetById(string taskId);
    }
}

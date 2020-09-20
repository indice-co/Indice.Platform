using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        public static BackgroundTaskInfo ToBackgroundTaskInfo(this Expression<Func<CancellationToken, Task>> expression) {
            return new BackgroundTaskInfo { };
        }
    }
}

using System;
using System.Collections.Generic;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// 
    /// </summary>
    public class BackgroundTaskInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The assembly name that the task resides.
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// The name of the method.
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<object> Arguments { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<Type> ArgumentTypes { get; set; }
        /// <summary>
        /// The return type of the method to call.
        /// </summary>
        public Type ReturnType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }
    }
}

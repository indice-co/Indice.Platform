using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The model that will be used as input to <see cref="IAwaitAssignmentInvoker"/>.
    /// </summary>
    public class AwaitAssignmentInvokerInput
    {
        /// <summary>
        /// The user the case will be assigned to.
        /// </summary>
        public AuditMeta User { get; set; }
    }
}
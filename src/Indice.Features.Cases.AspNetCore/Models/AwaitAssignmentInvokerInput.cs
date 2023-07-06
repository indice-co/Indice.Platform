using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Workflows.Interfaces;

namespace Indice.Features.Cases.Models;

/// <summary>The model that will be used as input to <see cref="IAwaitAssignmentInvoker"/>.</summary>
public class AwaitAssignmentInvokerInput
{
    /// <summary>The user the case will be assigned to.</summary>
    public AuditMeta User { get; set; }
    public bool SelfAssign { get; set; }
}
namespace Indice.Features.Cases.Core.Models;

/// <summary>The model that will be used as input to the workflow.</summary>
public class AwaitAssignmentInvokerInput
{
    /// <summary>The user the case will be assigned to.</summary>
    public AuditMeta User { get; set; } = null!;
}
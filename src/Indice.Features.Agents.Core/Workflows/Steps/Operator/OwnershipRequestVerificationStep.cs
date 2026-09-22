using Indice.Features.Agents.Core.Extensions;
using Microsoft.Agents.AI.Workflows;
using static Indice.Features.Agents.Core.Workflows.Demo.DemoWorkflow;
namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 2 of the Cases workflow: Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class OwnershipRequestVerificationStep : Executor<CaseRetrievalOutput, OwnershipVerificationRequestPort.OwnershipVerificationRequest>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="OwnershipRequestVerificationStep"/>.</summary>
    public OwnershipRequestVerificationStep(AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipRequestVerificationStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
    }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationRequestPort.OwnershipVerificationRequest> HandleAsync(
        CaseRetrievalOutput caseData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(caseData);
        var verificationFieldValue = caseData.VerificationValue ?? throw new InvalidOperationException($"Verification field not found in case data.");
        await context.Say(Id, _messageLocalizer.OwnershipVerificationMessagePrompt);
        return await ValueTask.FromResult(new OwnershipVerificationRequestPort.OwnershipVerificationRequest(_messageLocalizer.OwnershipVerificationMessagePrompt));
    }
}

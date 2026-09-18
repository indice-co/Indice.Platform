using Indice.Features.Agents.Core.Extensions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 2 of the Cases workflow: Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class OwnershipVerifierStep : Executor<CaseRetrievalOutput, ChatMessage>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="OwnershipVerifierStep"/>.</summary>
    public OwnershipVerifierStep(AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipVerifierStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
    }

    /// <inheritdoc/>
    public override async ValueTask<ChatMessage> HandleAsync(
        CaseRetrievalOutput caseData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(caseData);
        var verificationFieldValue = caseData.VerificationValue ?? throw new InvalidOperationException($"Verification field not found in case data.");
        //await context.AddEventAsync(new AnswerDeltaEvent(_messageLocalizer.OwnershipVerificationMessagePrompt +" comes from delta."), cancellationToken);
        return await ValueTask.FromResult(new ChatMessage(ChatRole.Assistant, [ new TextContent(_messageLocalizer.OwnershipVerificationMessagePrompt)]));
    }
}

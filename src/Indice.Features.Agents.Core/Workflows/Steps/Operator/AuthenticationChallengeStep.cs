using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class AuthenticationChallengeStep : Executor<OperationState, ChallengeRequestPort.ChallengeRequest>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="AuthenticationChallengeStep"/>.</summary>
    public AuthenticationChallengeStep(AgentMessageLocalizer messageLocalizer) : base(nameof(AuthenticationChallengeStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
    }

    /// <inheritdoc/>
    public override async ValueTask<ChallengeRequestPort.ChallengeRequest> HandleAsync(
        OperationState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(state);
        await context.Say(Id, _messageLocalizer.OwnershipVerificationMessagePrompt, cancellationToken);
        return await ValueTask.FromResult(new ChallengeRequestPort.ChallengeRequest(_messageLocalizer.OwnershipVerificationMessagePrompt));
    }
}

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Builds the next ownership verification challenge when validation allows a retry.
/// </summary>
public sealed class OwnershipRetryChallengeBuilder : Executor<UserInputValidationOutput, AgentResponseUpdate>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    /// <summary>Creates a new <see cref="OwnershipRetryChallengeBuilder"/>.</summary>
    public OwnershipRetryChallengeBuilder(AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipRetryChallengeBuilder)) {
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<AgentResponseUpdate> HandleAsync(
        UserInputValidationOutput validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(validationOutput);

        if (validationOutput.IsValid) {
            throw new InvalidOperationException("Ownership retry challenge requested for valid input.");
        }
        var retryPrompt = string.IsNullOrWhiteSpace(validationOutput.ErrorMessage)
            ? _messageLocalizer.OwnershipVerificationMessagePrompt
            : $"{validationOutput.ErrorMessage} {_messageLocalizer.OwnershipVerificationMessagePrompt}";
        return await ValueTask.FromResult(new AgentResponseUpdate(ChatRole.Assistant, retryPrompt));
    }
}
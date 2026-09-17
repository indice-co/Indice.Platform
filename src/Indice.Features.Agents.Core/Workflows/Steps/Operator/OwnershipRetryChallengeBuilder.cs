using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Builds the next ownership verification challenge when validation allows a retry.
/// </summary>
public sealed class OwnershipRetryChallengeBuilder : Executor<ChatMessage, ChatMessage>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    /// <summary>Creates a new <see cref="OwnershipRetryChallengeBuilder"/>.</summary>
    public OwnershipRetryChallengeBuilder(AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipRetryChallengeBuilder)) {
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<ChatMessage> HandleAsync(
        ChatMessage validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(validationOutput);
        var prompt = validationOutput.Contents.OfType<TextContent>().FirstOrDefault()?.Text;
        var retryPrompt = string.IsNullOrWhiteSpace(prompt)
            ? _messageLocalizer.OwnershipVerificationMessagePrompt
            : $"{prompt} {_messageLocalizer.OwnershipVerificationMessagePrompt}";

        return await ValueTask.FromResult(new ChatMessage(ChatRole.Assistant, retryPrompt));
    }
}
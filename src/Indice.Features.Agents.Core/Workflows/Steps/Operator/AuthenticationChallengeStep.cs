using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class AuthenticationChallengeStep : Executor<OperationState, ChallengeRequestPort.ChallengeRequest>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly ILogger<AuthenticationChallengeStep> _logger;

    /// <summary>Creates a new <see cref="AuthenticationChallengeStep"/>.</summary>
    public AuthenticationChallengeStep(AgentMessageLocalizer messageLocalizer, ILogger<AuthenticationChallengeStep> logger) : base(nameof(AuthenticationChallengeStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async ValueTask<ChallengeRequestPort.ChallengeRequest> HandleAsync(
        OperationState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(state);
        _logger.LogDebug("Requesting ownership challenge from user.");
        await context.Say(Id, _messageLocalizer.OwnershipVerificationMessagePrompt, cancellationToken);
        return await ValueTask.FromResult(new ChallengeRequestPort.ChallengeRequest(_messageLocalizer.OwnershipVerificationMessagePrompt));
    }
}

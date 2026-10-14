using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Services.Validation;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Validates user's ownership confirmation input.
/// Receives the user's reply from the ownership request port and
/// compares it with the actual case data field. Supports up to <see cref="CustomerWorkflowOptions.MaxOwnershipValidationAttempts"/> validation attempts.
/// </summary>
[SendsMessage(typeof(ChallengeRequestPort.ChallengeRequest))]
[SendsMessage(typeof(OperationState))]
[YieldsOutput(typeof(OperationState))]
public sealed class AuthenticationStep : Executor<ChallengeRequestPort.ChallengeResponse>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly int _maxValidationAttempts;
    private readonly ILogger<AuthenticationStep> _logger;
    private readonly ChallengeValidatorFactory _validatorFactory;

    /// <summary>Creates a new <see cref="AuthenticationStep"/>.</summary>
    public AuthenticationStep(AgentMessageLocalizer messageLocalizer, IOptions<CustomerWorkflowOptions> options, ChallengeValidatorFactory validatorFactory, ILogger<AuthenticationStep> logger) : base(nameof(AuthenticationStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxValidationAttempts = options.Value.MaxOwnershipValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        ChallengeRequestPort.ChallengeResponse confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(confirmation);
        var userInput = confirmation.userInput;

        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var verificationData = caseData.ChallengeValue!;
        if (string.IsNullOrWhiteSpace(verificationData)) {
            _logger.LogWarning("No challenge value found in operator state for case {ReferenceId}; ownership validation will fail.", caseData.ReferenceId);
        }

        // Validate the input against the actual case field value
        var isValid = _validatorFactory.Create(caseData.DataType).IsValid(userInput, verificationData);

        if (!isValid) {
            var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
            await context.SetApprovalStateAsync(attempt, cancellationToken);
            if (attempt >= _maxValidationAttempts) {
                await context.SetApprovalStateAsync(null, cancellationToken);
                await context.Say(Id, _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts));
                await context.YieldOutputAsync(OperationState.End);
                return;
            }
            await context.Say(Id, _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts));
            await context.SendMessageAsync(new ChallengeRequestPort.ChallengeRequest(_messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts)));
            return;
        }
        await context.SetApprovalStateAsync(null, cancellationToken);
        await context.Say(Id, _messageLocalizer.ChallengeSucceeded);
        await context.SendMessageAsync(OperationState.Next(nameof(OtpCodeSendStep)));
    }
}


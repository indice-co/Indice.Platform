using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.RequestPorts;

/// <summary>Handles OTP verification request-port events.</summary>
public sealed class OtpVerificationRequestPortHandler : IRequestPortHandler
{
    /// <inheritdoc/>
    public async ValueTask<RequestPortHandlerResult> HandleAsync(RequestInfoEvent requestInfoEvent, RequestPortHandlerContext context, CancellationToken cancellationToken = default) {
        if (requestInfoEvent.Request.PortInfo.PortId != AgentsConstants.WorkflowPorts.OtpVerification
            || !requestInfoEvent.Request.TryGetDataAs<OtpChallengeOutput>(out var otpChallenge)) {
            return RequestPortHandlerResult.NotHandled;
        }

        if (context.UserReply is not null) {
            await context.Run.SendResponseAsync(requestInfoEvent.Request.CreateResponse(new OtpCodeResponse(otpChallenge!, context.UserReply)));
            context.UserReply = null;
            return RequestPortHandlerResult.Continue;
        }

        return RequestPortHandlerResult.Halt(
            new ChatResponseUpdate(ChatRole.Assistant, otpChallenge!.Prompt) {
                ConversationId = context.ConversationId
            });
    }
}

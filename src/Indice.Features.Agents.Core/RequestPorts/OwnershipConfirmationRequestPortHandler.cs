using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.RequestPorts;

/// <summary>Handles ownership confirmation request-port events.</summary>
public sealed class OwnershipConfirmationRequestPortHandler : IRequestPortHandler
{
    /// <inheritdoc/>
    public async ValueTask<RequestPortHandlerResult> HandleAsync(RequestInfoEvent requestInfoEvent, RequestPortHandlerContext context, CancellationToken cancellationToken = default) {
        if (requestInfoEvent.Request.PortInfo.PortId != AgentsConstants.WorkflowPorts.OwnershipConfirmation
            || !requestInfoEvent.Request.TryGetDataAs<OwnershipVerificationOutput>(out var verificationData)) {
            return RequestPortHandlerResult.NotHandled;
        }

        if (context.UserReply is not null) {
            await context.Run.SendResponseAsync(requestInfoEvent.Request.CreateResponse(new OwnershipConfirmationResponse(verificationData!, context.UserReply)));
            context.UserReply = null;
            return RequestPortHandlerResult.Continue;
        }

        return RequestPortHandlerResult.Halt(
            new ChatResponseUpdate(ChatRole.Assistant, verificationData!.VerificationPrompt) {
                ConversationId = context.ConversationId
            });
    }
}

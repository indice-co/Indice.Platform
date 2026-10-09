using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
[SendsMessage(typeof(UserInputRequestPort.UserInputRequest))]
[SendsMessage(typeof(ChatMessage))]
internal sealed class CaseReferenceResolverStep : Executor<OperationRequestPort.OperationResponse>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="CaseReferenceResolverStep"/>.</summary>
    public CaseReferenceResolverStep(AgentMessageLocalizer messageLocalizer) : base(nameof(CaseReferenceResolverStep)) {
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        OperationRequestPort.OperationResponse operationResponse,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operationResponse);
        var state = await context.GetConversationStateAsync(cancellationToken);
        state.Message.AdditionalProperties ??= new();
        state.Message.AdditionalProperties[nameof(OperationRequestPort.OperationResponse)] = operationResponse;
        await context.SetConversationStateAsync(state, cancellationToken);
        if (state.Message.AdditionalProperties.ContainsKey(nameof(ChatTopic))) {
            await context.SendMessageAsync(state.Message, cancellationToken);
            return;
        }
        await context.Say(Id, _messageLocalizer.AskReferenceNumber, cancellationToken);
        await context.SendMessageAsync(new UserInputRequestPort.UserInputRequest(_messageLocalizer.AskReferenceNumber), cancellationToken);
    }
}

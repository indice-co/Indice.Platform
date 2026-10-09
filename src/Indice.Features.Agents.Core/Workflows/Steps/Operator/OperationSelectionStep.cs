
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
[SendsMessage(typeof(OperationRequestPort.OperationRequest))]
[SendsMessage(typeof(ChatMessage))]
internal sealed class OperationSelectionStep : Executor<ChatMessage>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    /// <summary>Creates a new <see cref="OperationSelectionStep"/>.</summary>
    public OperationSelectionStep(AgentMessageLocalizer messageLocalizer) : base(nameof(OperationSelectionStep)) {
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        // Persist a ConversationState snapshot so downstream shared steps (e.g. OtpAgent)
        // can use existing state extension helpers.
        await context.SetConversationStateAsync(new ConversationState(message, message.AdditionalProperties![nameof(ConversationState.ConversationId)]!.ToString()!), cancellationToken);
        
        //var userInput = message.Text ?? string.Empty;
        if (message.AdditionalProperties.TryGetValue<ChatTopic>(nameof(ChatTopic), out var additional) && !string.IsNullOrWhiteSpace(additional.ReferenceId)) {
            await context.Say(Id, _messageLocalizer.OperatorWelcomeKnownCase);
            await context.SendMessageAsync(message, cancellationToken);
            return;
        }
        await context.Say(Id, _messageLocalizer.OperatorWelcomeUknownCase);
        await context.SendMessageAsync(new OperationRequestPort.OperationRequest([new("ServicePickUp", "Service PickUp"),new("Appointment", "Book a service appointment")]), cancellationToken);
    }
}
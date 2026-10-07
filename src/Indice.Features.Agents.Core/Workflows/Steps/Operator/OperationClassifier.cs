
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
internal sealed class OperationClassifier : Executor<ChatMessage>
{
    /// <summary>Creates a new <see cref="OperationClassifier"/>.</summary>
    public OperationClassifier() : base(nameof(OperationClassifier)) {
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
        if (message.AdditionalProperties.TryGetValue<ChatTopic>(nameof(ChatTopic), out var additional) && !string.IsNullOrEmpty(additional.ReferenceType)) {
            await context.Say(Id, "Hello I am your Digital assistant. I am retrieving you data.");
            await context.SendMessageAsync(message, cancellationToken);
            return;
        }
        await context.Say(Id, "Hello I am your Digital assistant. With what can I help you?");
        await context.SendMessageAsync(new OperationRequestPort.OperationRequest(["Service PickUp", "Appointment"]), cancellationToken);
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Protocol;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
internal sealed class OperationClassifier : Executor<ChatMessage, OperationRequestPort.OperationRequest>
{
    /// <summary>Creates a new <see cref="OperationClassifier"/>.</summary>
    public OperationClassifier() : base(nameof(OperationClassifier)) {
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationRequestPort.OperationRequest> HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);

        // Persist a ConversationState snapshot so downstream shared steps (e.g. OtpAgent)
        // can use existing state extension helpers.
        await context.SetConversationStateAsync(new ConversationState(message, message.AdditionalProperties![nameof(ConversationState.ConversationId)]!.ToString()!), cancellationToken);
        //var userInput = message.Text ?? string.Empty;
        //if (message.AdditionalProperties.TryGetValue<ChatTopic>(nameof(ChatTopic), out var additional) && !string.IsNullOrEmpty(additional.ReferenceType)) {
        //    return OperationState.Next(nameof(DataRetrieverStep));
        //}
        await context.Say(Id, "What can I help you with?");
        return new OperationRequestPort.OperationRequest(["Service PickUp", "Appointment"]);
    }
}
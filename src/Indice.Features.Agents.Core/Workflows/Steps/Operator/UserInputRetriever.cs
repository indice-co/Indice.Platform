using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
[SendsMessage(typeof(UserInputRequestPort.UserInputRequest))]
[SendsMessage(typeof(ChatMessage))]
internal sealed class UserInputRetriever : Executor<OperationRequestPort.OperationResponse>
{

    /// <summary>Creates a new <see cref="UserInputRetriever"/>.</summary>
    public UserInputRetriever() : base(nameof(UserInputRetriever)) {
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        OperationRequestPort.OperationResponse operationResponse,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operationResponse);
        var state = await context.GetConversationStateAsync(cancellationToken);
        var message = state.Message;

        if (message.AdditionalProperties?.ContainsKey(nameof(ChatTopic)) == true) {
            await context.SendMessageAsync(message, cancellationToken);
            return;
        }

        await context.Say(Id, "Please type your case id.", cancellationToken);
        await context.SendMessageAsync(new UserInputRequestPort.UserInputRequest("Please type your case id."), cancellationToken);
    }
}

/// <summary>Turns the user's free-text answer into the <see cref="ChatMessage"/> consumed by the next step.</summary>
internal sealed class UserInputCollector : Executor<UserInputRequestPort.UserResponse, ChatMessage>
{
    /// <summary>Creates a new <see cref="UserInputCollector"/>.</summary>
    public UserInputCollector() : base(nameof(UserInputCollector)) {
    }

    /// <inheritdoc/>
    public override async ValueTask<ChatMessage> HandleAsync(
        UserInputRequestPort.UserResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);
        var state = await context.GetConversationStateAsync(cancellationToken);
        return new ChatMessage(ChatRole.User, response.InputMessage) {
            AdditionalProperties = state.Message.AdditionalProperties
        };
    }
}
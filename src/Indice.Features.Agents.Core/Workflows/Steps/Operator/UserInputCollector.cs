using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

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
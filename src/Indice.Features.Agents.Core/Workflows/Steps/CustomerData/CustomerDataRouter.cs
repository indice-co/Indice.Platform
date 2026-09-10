using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Entry step of the customer-data sub-workflow. Rehydrates the durable <see cref="CustomerDataState"/> of the
/// conversation — seeded either by the external reference the host UI passed on creation, or by an earlier
/// turn — and hands it to the rest of the pipeline, which resumes at <see cref="CustomerDataState.Stage"/>.
/// </summary>
public sealed class CustomerDataRouter : CustomerDataStep<ConversationState>
{
    /// <summary>Creates a new <see cref="CustomerDataRouter"/>.</summary>
    public CustomerDataRouter(IOptions<AgentsOptions> options, IWorkflowStateStore store) : base(nameof(CustomerDataRouter), options.Value, store) { }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(ConversationState message, IWorkflowContext context, CancellationToken cancellationToken = default) {
        await context.SetConversationStateAsync(message, cancellationToken);
        var conversationId = Guid.Parse(message.ConversationId);
        var state = await Store.LoadAsync<CustomerDataState>(conversationId, cancellationToken) ?? new CustomerDataState();
        if (state.Stage == CustomerDataStage.Blocked) {
            await SayAsync(context, Settings.BlockedMessage, cancellationToken);
            return new CustomerDataTurn { State = state, Continue = false, Answer = Settings.BlockedMessage };
        }
        state = await PersistAsync(context, state, conversationId, cancellationToken);
        return new CustomerDataTurn { State = state, Continue = true };
    }
}

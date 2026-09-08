using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Retrieves the payload behind the external reference through <see cref="ICustomerDataResolver"/>, on every
/// turn. Keeping the retrieval — rather than the data — durable is what allows the loop to resume without ever
/// persisting undisclosed customer data in the chat database.
/// </summary>
public sealed class CustomerDataRetriever : CustomerDataStep<CustomerDataTurn>
{
    private readonly ICustomerDataResolver _resolver;

    /// <summary>Creates a new <see cref="CustomerDataRetriever"/>.</summary>
    public CustomerDataRetriever(IOptions<AgentsOptions> options, IWorkflowStateStore store, ICustomerDataResolver resolver)
        : base(nameof(CustomerDataRetriever), options.Value, store) {
        _resolver = resolver;
    }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(CustomerDataTurn turn, IWorkflowContext context, CancellationToken cancellationToken = default) {
        var (conversationId, _) = await GetConversationAsync(context, cancellationToken);
        if (turn.State.GetReference() is not { } reference) {
            return turn with { Continue = false };
        }
        var record = await _resolver.ResolveAsync(reference, cancellationToken);
        if (record is null) {
            // Drop the reference so the next turn asks for it again instead of retrying a dead lookup forever.
            var cleared = await PersistAsync(context, turn.State with {
                ReferenceId = null,
                Stage = CustomerDataStage.CollectReference,
                IdentityVerified = false,
                CodeVerified = false,
                ChannelKind = null,
                ChannelPath = null
            }, conversationId, cancellationToken);
            var message = string.Format(Settings.ReferenceNotFoundMessage, reference.Id);
            await SayAsync(context, message, cancellationToken);
            return new CustomerDataTurn { State = cleared, Continue = false, Answer = message };
        }
        var state = await PersistAsync(context, turn.State with { ReferenceType = string.IsNullOrWhiteSpace(turn.State.ReferenceType) ? record.DataType : turn.State.ReferenceType }, conversationId, cancellationToken);
        return new CustomerDataTurn { State = state, Data = record, Continue = true };
    }
}

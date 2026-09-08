using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Cards;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Terminal step of the customer-data sub-workflow: renders the verified payload as an HTML card part the chat
/// surface can display, using the template registered for the data type.
/// </summary>
public sealed class CustomerDataCardPresenter : CustomerDataStep<CustomerDataTurn>
{
    private readonly ICustomerDataCardRenderer _renderer;

    /// <summary>Creates a new <see cref="CustomerDataCardPresenter"/>.</summary>
    public CustomerDataCardPresenter(IOptions<AgentsOptions> options, IWorkflowStateStore store, ICustomerDataCardRenderer renderer)
        : base(nameof(CustomerDataCardPresenter), options.Value, store) {
        _renderer = renderer;
    }

    /// <inheritdoc/>
    public override async ValueTask<CustomerDataTurn> HandleAsync(CustomerDataTurn turn, IWorkflowContext context, CancellationToken cancellationToken = default) {
        // Defence in depth: the edges only lead here once verified, but the card is the disclosure point.
        if (turn.Data is null || !turn.State.IdentityVerified || !turn.State.CodeVerified) {
            return turn with { Continue = false };
        }
        var (conversationId, _) = await GetConversationAsync(context, cancellationToken);
        var message = string.Format(Settings.PresentationMessage, turn.Data.Reference.Id);
        await SayAsync(context, message, cancellationToken);
        await SayAsync(context, _renderer.Render(turn.Data), cancellationToken);
        var state = await PersistAsync(context, turn.State with { Stage = CustomerDataStage.Completed }, conversationId, cancellationToken);
        return new CustomerDataTurn { State = state, Data = turn.Data, Continue = false, Answer = message };
    }
}

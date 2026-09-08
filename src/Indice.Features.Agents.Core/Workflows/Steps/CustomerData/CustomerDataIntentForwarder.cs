using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// Adapter used by the auto-routing workflow: when the master intent classifier decides the user needs
/// something done with external customer data, this hands the conversation back to
/// <see cref="CustomerDataRouter"/>, which is the entry point of the customer-data sub-workflow.
/// </summary>
public sealed class CustomerDataIntentForwarder : Executor<IntentOutput, ConversationState>
{
    /// <summary>Creates a new <see cref="CustomerDataIntentForwarder"/>.</summary>
    public CustomerDataIntentForwarder() : base(nameof(CustomerDataIntentForwarder)) { }

    /// <inheritdoc/>
    public override async ValueTask<ConversationState> HandleAsync(IntentOutput intent, IWorkflowContext context, CancellationToken cancellationToken = default)
        => await context.GetConversationStateAsync(cancellationToken);
}

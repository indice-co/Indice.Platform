using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.CustomerData;

/// <summary>
/// The envelope travelling the customer-data pipeline. Every step reads the durable
/// <see cref="CustomerDataState"/> off it, and sets <see cref="Continue"/> to <c>false</c> when the flow needs
/// something from the user — the conditional edges then stop the run, leaving the conversation parked at
/// <see cref="CustomerDataState.Stage"/> until the next turn.
/// </summary>
public record CustomerDataTurn
{
    /// <summary>The durable state as known to the step that produced this envelope.</summary>
    public required CustomerDataState State { get; init; }

    /// <summary>The retrieved payload, once a step has resolved it. Never persisted.</summary>
    public CustomerDataRecord? Data { get; init; }

    /// <summary>Whether the flow can proceed to the next step within this turn.</summary>
    public bool Continue { get; init; }

    /// <summary>The text the step said to the user, if any. Mirrors what was streamed, for callers that inspect the result.</summary>
    public string? Answer { get; init; }
}

/// <summary>
/// Base class of the customer-data steps: gives them the conversation they run for, the durable state store,
/// and a single way of speaking to the user.
/// </summary>
/// <typeparam name="TInput">The message type the step handles.</typeparam>
public abstract class CustomerDataStep<TInput> : Executor<TInput, CustomerDataTurn> where TInput : notnull
{
    /// <summary>Creates a new step.</summary>
    /// <param name="id">The executor id, also the resume point recorded in the durable state.</param>
    /// <param name="options">The agents options.</param>
    /// <param name="store">The durable workflow state store.</param>
    protected CustomerDataStep(string id, AgentsOptions options, IWorkflowStateStore store) : base(id) {
        Settings = options.CustomerData;
        Store = store;
    }

    /// <summary>Customer-data knobs and wording.</summary>
    protected AgentsOptions.CustomerDataOptions Settings { get; }

    /// <summary>Durable per-conversation state store.</summary>
    protected IWorkflowStateStore Store { get; }

    /// <summary>Streams <paramref name="text"/> to the user as an assistant update.</summary>
    protected Task SayAsync(IWorkflowContext context, string text, CancellationToken cancellationToken)
        => context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, text)), cancellationToken).AsTask();

    /// <summary>Streams the atomic <paramref name="content"/> part to the user.</summary>
    protected Task SayAsync(IWorkflowContext context, AIContent content, CancellationToken cancellationToken)
        => context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [content])), cancellationToken).AsTask();

    /// <summary>
    /// Records this step as the resume point, then writes the state to both the in-run workflow context and the
    /// conversation row, so the next turn picks up where this one stopped.
    /// </summary>
    protected async Task<CustomerDataState> PersistAsync(IWorkflowContext context, CustomerDataState state, Guid conversationId, CancellationToken cancellationToken) {
        var stamped = state with { LastStepId = Id };
        await context.SetCustomerDataStateAsync(stamped, conversationId, Store, cancellationToken);
        return stamped;
    }

    /// <summary>The conversation this run belongs to.</summary>
    protected static async Task<(Guid Id, string Text)> GetConversationAsync(IWorkflowContext context, CancellationToken cancellationToken) {
        var conversation = await context.GetConversationStateAsync(cancellationToken);
        return (Guid.Parse(conversation.ConversationId), conversation.Message.Text ?? string.Empty);
    }
}

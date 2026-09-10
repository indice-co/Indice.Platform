using Indice.Features.Agents.Core.Services;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>Extension methods for <see cref="IWorkflowContext"/>.</summary>
public static class IWorkflowContextStateExtensions
{
    /// <summary>The scope name used to store the <see cref="ConversationState"/> in the workflow context.</summary>
    public const string ConversationScope = "ConversationScope";
    /// <summary>Reads the <see cref="ConversationState"/> from the workflow context.</summary>
    public static async Task<ConversationState> GetConversationStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) {
        return await context.ReadStateAsync<ConversationState>(nameof(ConversationState), scopeName: ConversationScope, cancellationToken: cancellationToken) ??
               throw new InvalidOperationException("ConversationState not found in workflow context.");
    }

    /// <summary>Writes the <see cref="ConversationState"/> to the workflow context.</summary>
    public static async Task SetConversationStateAsync(this IWorkflowContext context, ConversationState state, CancellationToken cancellationToken = default) {
        await context.QueueStateUpdateAsync(nameof(ConversationState), state, scopeName: ConversationScope, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the <see cref="IntentState"/> from the workflow context.</summary>
    public static async Task<IntentState> GetIntentStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) {
        return await context.ReadStateAsync<IntentState>(nameof(IntentState), scopeName: ConversationScope, cancellationToken: cancellationToken) ??
               throw new InvalidOperationException("IntentState not found in workflow context.");
    }

    /// <summary>Writes the <see cref="IntentState"/> to the workflow context.</summary>
    public static async Task SetIntentStateAsync(this IWorkflowContext context, IntentState state, CancellationToken cancellationToken = default) {
        await context.QueueStateUpdateAsync(nameof(IntentState), state, scopeName: ConversationScope, cancellationToken: cancellationToken);
    }

    /// <summary>Reads the <see cref="CustomerDataState"/> from the workflow context, or a fresh one when the run has not seeded it yet.</summary>
    public static async Task<CustomerDataState> GetCustomerDataStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) {
        return await context.ReadStateAsync<CustomerDataState>(nameof(CustomerDataState), scopeName: ConversationScope, cancellationToken: cancellationToken) ??
               new CustomerDataState();
    }

    /// <summary>
    /// Writes the <see cref="CustomerDataState"/> to the workflow context and — when a <paramref name="store"/> is
    /// supplied — durably to the conversation, so the next turn resumes at <see cref="CustomerDataState.Stage"/>.
    /// </summary>
    public static async Task SetCustomerDataStateAsync(
        this IWorkflowContext context, CustomerDataState state, Guid conversationId,
        IWorkflowStateStore? store = null, CancellationToken cancellationToken = default) {
        await context.QueueStateUpdateAsync(nameof(CustomerDataState), state, scopeName: ConversationScope, cancellationToken: cancellationToken);
        if (store is not null) {
            await store.SaveAsync(conversationId, state, cancellationToken);
        }
    }
}


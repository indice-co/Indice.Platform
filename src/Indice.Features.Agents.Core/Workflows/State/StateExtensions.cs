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
}


using Indice.Features.Agents.Core.Workflows.Steps.Operator;
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

    /// <summary>Reads the <see cref="CustomerState"/> from the workflow context.</summary>
    public static async Task<CustomerState> GetOperatorStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) {
        return await context.ReadStateAsync<CustomerState>(nameof(CustomerState), scopeName: ConversationScope, cancellationToken: cancellationToken) ??
               throw new InvalidOperationException("CustomerState not found in workflow context.");
    }

    /// <summary>Writes the <see cref="CustomerState"/> to the workflow context.</summary>
    public static async Task SetOperatorStateAsync(this IWorkflowContext context, CustomerState state, CancellationToken cancellationToken = default) {
        await context.QueueStateUpdateAsync(nameof(CustomerState), state, scopeName: ConversationScope, cancellationToken: cancellationToken);
    }
    /// <summary>Reads the <see cref="AuthenticationStep"/> from the workflow context.</summary>
    public static async Task<int> GetApprovalStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) {
        return await context.ReadStateAsync<int?>(nameof(AuthenticationStep), scopeName: ConversationScope, cancellationToken: cancellationToken) ?? 0;
    }

    /// <summary>Writes the <see cref="AuthenticationStep"/> to the workflow context.</summary>
    public static async Task SetApprovalStateAsync(this IWorkflowContext context, int? state, CancellationToken cancellationToken = default) {
        await context.QueueStateUpdateAsync(nameof(AuthenticationStep), state, scopeName: ConversationScope, cancellationToken: cancellationToken);
    }
}


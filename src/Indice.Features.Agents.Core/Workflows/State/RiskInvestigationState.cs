using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>
/// What the risk investigation has collected from the user so far. It is kept across the pauses where the agent asks for a
/// missing detail, so the user can answer "the 12th of November" without repeating the transaction number.
/// </summary>
/// <param name="Tun">The transaction number, once given and valid.</param>
/// <param name="TransactionDate">The date, <c>yyyy-MM-dd</c>, once given and valid.</param>
/// <param name="Language">The language the user writes in, once known.</param>
/// <param name="Purpose">What the report is for, if the user said.</param>
/// <param name="Attempts">How many times the agent has asked for details or the transaction was not found.</param>
public sealed record RiskInvestigationState(string? Tun = null, string? TransactionDate = null, string? Language = null, string? Purpose = null, int Attempts = 0);

/// <summary>Reads and writes <see cref="RiskInvestigationState"/> in the workflow's conversation scope.</summary>
public static class RiskInvestigationStateExtensions
{
    /// <summary>The state so far, or an empty one at the start of an investigation.</summary>
    public static async Task<RiskInvestigationState> GetRiskStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) =>
        await context.ReadStateAsync<RiskInvestigationState>(nameof(RiskInvestigationState), scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken)
        ?? new RiskInvestigationState();

    /// <summary>Stores the state.</summary>
    public static async Task SetRiskStateAsync(this IWorkflowContext context, RiskInvestigationState state, CancellationToken cancellationToken = default) =>
        await context.QueueStateUpdateAsync(nameof(RiskInvestigationState), state, scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken);

    /// <summary>The conversation state if the run has already stored it, or <c>null</c> on the first pass.</summary>
    public static async Task<ConversationState?> TryGetConversationStateAsync(this IWorkflowContext context, CancellationToken cancellationToken = default) =>
        await context.ReadStateAsync<ConversationState>(nameof(ConversationState), scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken);
}

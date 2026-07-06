using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Events;

/// <summary>
/// Custom workflow event reporting the token usage of a single LLM call, emitted by the reasoning steps
/// after their agent run completes. Observed by <c>DexRunner</c> on both run paths, which folds all
/// occurrences into one <see cref="UsageDetails"/> total via <see cref="UsageDetails.Add"/>.
/// </summary>
public sealed class UsageEvent : WorkflowEvent
{
    /// <summary>Creates a new <see cref="UsageEvent"/> carrying the usage of one LLM call.</summary>
    public UsageEvent(UsageDetails details, string model) : base(details) {
        Details = details;
        Model = model;
    }

    /// <summary>Token usage reported by the model for this call.</summary>
    public UsageDetails Details { get; }

    /// <summary>The model deployment the usage was billed against.</summary>
    public string Model { get; }
}

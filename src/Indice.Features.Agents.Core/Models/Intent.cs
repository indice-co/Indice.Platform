namespace Indice.Features.Agents.Core.Models;

/// <summary>Classified intent of a user question. Drives early-exit on out-of-scope and supplies retrieval filters.</summary>
public class Intent
{
    /// <summary>Short intent label (e.g. <c>question</c>, <c>greeting</c>, <c>command</c>).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Validated category from <c>DexOptions.Taxonomy.Categories</c>, or null if no confident match.</summary>
    public string? Category { get; init; }

    /// <summary>Validated language code from <c>DexOptions.Taxonomy.Languages</c>, or null if no confident match.</summary>
    public string? Language { get; init; }

    /// <summary>When false, the pipeline early-exits with <see cref="OutOfScopeReason"/>.</summary>
    public bool IsInScope { get; init; }

    /// <summary>Polite human-readable reason when <see cref="IsInScope"/> is false.</summary>
    public string? OutOfScopeReason { get; init; }

    /// <summary>
    /// When true the user is after a specific record of their own held in an external system (a case, an order,
    /// a service pickup) rather than an answer from the knowledge base, and the auto-routing workflow hands the
    /// turn to the customer-data sub-workflow — which verifies them before disclosing anything.
    /// </summary>
    public bool RequiresCustomerData { get; init; }
}

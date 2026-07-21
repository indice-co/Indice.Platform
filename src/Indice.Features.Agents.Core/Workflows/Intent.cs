namespace Indice.Features.Agents.Core.Workflows;

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
}

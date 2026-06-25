namespace Indice.Features.Agents.Core.Workflows;

/// <summary>Optional filters applied at retrieval time, derived from the classified <see cref="Intent"/>.</summary>
public class RetrievalFilters
{
    /// <summary>Restrict candidates to chunks in this category, or null for no category filter.</summary>
    public string? Category { get; init; }

    /// <summary>Restrict candidates to chunks in this language, or null for no language filter.</summary>
    public string? Language { get; init; }
}

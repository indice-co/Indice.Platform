namespace Indice.Features.Agents.Core.Models;

/// <summary>Optional filters applied at retrieval time, derived from the classified <see cref="Intent"/>.</summary>
public class RetrievalFilters
{
    /// <summary>
    /// Restrict candidates to chunks in this category, or null for no category filter. Chunks that have no category always
    /// match, except when the category is a restricted one (see <see cref="IncludeRestricted"/>), where only chunks of exactly
    /// that category match.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>Restrict candidates to chunks in this language, or null for no language filter.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// Also return chunks of the categories listed in <c>Dex:Taxonomy:RestrictedCategories</c>. Defaults to <c>false</c>, and
    /// nothing derived from user input or model output sets it: it is meant for workflow code that has already checked the
    /// caller is allowed to read that knowledge (for example an agent with <see cref="AgentInfo.RequiredRoles"/>).
    /// </summary>
    public bool IncludeRestricted { get; init; }
}

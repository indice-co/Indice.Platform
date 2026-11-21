namespace Indice.Features.Agents.Core.Models;

/// <summary>A section of the knowledge base read by its heading rather than found by similarity.</summary>
/// <param name="HeadingPath">The heading breadcrumb, for example <c>Limitations and data quality &gt; Coverage &gt; L-01 The Risk API may not have been collecting</c>.</param>
/// <param name="Text">The section's text, without the heading breadcrumb the chunker prepends to every chunk.</param>
/// <param name="Category">The category of the document.</param>
public sealed record KnowledgeSnippet(string HeadingPath, string Text, string? Category);

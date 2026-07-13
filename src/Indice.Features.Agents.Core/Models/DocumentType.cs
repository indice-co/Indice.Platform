namespace Indice.Features.Agents.Core.Models;

/// <summary>Represents the type of a document.</summary>
public enum DocumentType
{
    /// <summary>A Markdown FAQ document — a flat list of <c>## Question</c> / answer pairs parsed one chunk per pair.</summary>
    MarkdownFaq = 0,
    /// <summary>A general-structure Markdown document — chunked by heading section with a token-budget fallback.</summary>
    Markdown = 1
}

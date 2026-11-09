using System.Security.Cryptography;
using System.Text;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Indice.Features.Agents.Core.Workflows.Ingestion;

/// <summary>
/// Structural, heading-aware chunker for general Markdown. Walks the Markdig AST in source order, tracking a
/// heading breadcrumb, and emits one chunk per heading section — splitting oversized sections by a character budget
/// with overlap. Content with no enclosing heading (a flat file, or intro prose above the first heading) is
/// anchored to <c>documentTitle</c>. Original source text is sliced verbatim, so code fences, tables, and lists
/// survive intact.
/// </summary>
public static class MarkdownChunker
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    /// <summary>Splits <paramref name="body"/> into structural chunks.</summary>
    /// <param name="body">The raw Markdown text.</param>
    /// <param name="documentTitle">Breadcrumb root for content that has no heading above it.</param>
    /// <param name="options">Ingestion knobs (<see cref="AgentsOptions.IngestionOptions.ChunkTargetChars"/> / <see cref="AgentsOptions.IngestionOptions.ChunkOverlapChars"/>).</param>
    public static IReadOnlyList<DocumentChunk> Chunk(string body, string documentTitle, AgentsOptions.IngestionOptions options) {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrWhiteSpace(body)) {
            return chunks;
        }

        var document = Markdown.Parse(body, Pipeline);
        var headings = new List<(int Level, string Text)>();
        int? sectionStart = null;
        var sectionEnd = 0;
        var chunkIndex = 0;

        foreach (var block in document) {
            if (block is YamlFrontMatterBlock) {
                continue;
            }
            if (block is HeadingBlock heading) {
                Flush();
                while (headings.Count > 0 && headings[^1].Level >= heading.Level) {
                    headings.RemoveAt(headings.Count - 1);
                }
                headings.Add((heading.Level, HeadingText(heading)));
                continue;
            }
            if (block.Span.Length <= 0) {
                continue;
            }
            sectionStart ??= block.Span.Start;
            sectionEnd = block.Span.End;
        }
        Flush();
        return chunks;

        void Flush() {
            if (sectionStart is null) {
                return;
            }
            var text = body.Substring(sectionStart.Value, sectionEnd - sectionStart.Value + 1).Trim('\r', '\n');
            sectionStart = null;
            if (text.Length == 0) {
                return;
            }
            var breadcrumb = headings.Count > 0 ? string.Join(" > ", headings.Select(h => h.Text)) : documentTitle;
            var title = headings.Count > 0 ? headings[^1].Text : documentTitle;
            foreach (var piece in SplitText(text, options).Where(piece => piece.Length > 0)) {
                Emit(breadcrumb, title, piece);
            }
        }

        void Emit(string headingPath, string title, string text) {
            var content = $"{headingPath}\n\n{text}";
            chunks.Add(new DocumentChunk {
                ChunkIndex = chunkIndex++,
                Content = content,
                ContentHash = Sha256Hex(content),
                HeadingPath = headingPath,
                Title = title,
                Category = null,
                TokenCount = EstimateTokens(content),
            });
        }
    }

    /// <summary>
    /// Extracts the plain-text label of a heading from its parsed inline tree — correct for both ATX (<c>## Foo</c>)
    /// and setext (<c>Foo</c> underlined with <c>===</c>) styles, and strips inline markup so <c>## **Bold** `code`</c>
    /// yields <c>Bold code</c>.
    /// </summary>
    private static string HeadingText(HeadingBlock heading) {
        if (heading.Inline is null) {
            return string.Empty;
        }
        var builder = new StringBuilder();
        foreach (var inline in heading.Inline.Descendants()) {
            switch (inline) {
                case LiteralInline literal:
                    builder.Append(literal.Content);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
            }
        }
        return builder.ToString().Trim();
    }

    /// <summary>
    /// Splits <paramref name="text"/> into windows of at most <see cref="AgentsOptions.IngestionOptions.ChunkTargetChars"/> characters,
    /// each seeded with a ~<see cref="AgentsOptions.IngestionOptions.ChunkOverlapChars"/>-character tail of the previous one. Cuts fall on
    /// whitespace-run boundaries — which coincide with paragraph, sentence, and word boundaries — preferring the furthest one
    /// that fits the budget; a hard character limit guarantees a bounded chunk even for an unbroken wall of text.
    /// </summary>
    private static IReadOnlyList<string> SplitText(string text, AgentsOptions.IngestionOptions options) {
        var target = Math.Max(1, options.ChunkTargetChars);
        if (text.Length <= target) {
            return [text];
        }
        var overlap = Math.Clamp(options.ChunkOverlapChars, 0, target - 1);

        // Candidate cut offsets: the position after each maximal whitespace run, plus the end of the text.
        var breaks = new List<int>();
        for (var i = 0; i < text.Length; i++) {
            if (char.IsWhiteSpace(text[i])) {
                while (i < text.Length && char.IsWhiteSpace(text[i])) {
                    i++;
                }
                breaks.Add(i);
                i--;
            }
        }
        if (breaks.Count == 0 || breaks[^1] != text.Length) {
            breaks.Add(text.Length);
        }

        var windows = new List<string>();
        var start = 0;
        while (start < text.Length) {
            int end;
            if (start + target >= text.Length) {
                end = text.Length;
            } else {
                var boundary = FloorBreak(start, start + target);
                end = boundary > start ? boundary : Math.Min(start + target, text.Length);
            }
            var slice = text[start..end].Trim('\r', '\n');
            if (slice.Length > 0) {
                windows.Add(slice);
            }
            if (end >= text.Length) {
                break;
            }
            // Back up ~overlap characters to a boundary to seed the next window; never regress past start.
            var nextStart = end;
            if (overlap > 0) {
                var boundary = FloorBreak(start, end - overlap);
                if (boundary > start) {
                    nextStart = boundary;
                }
            }
            start = nextStart > start ? nextStart : end;
        }
        return windows;

        // Largest break in (start, ceiling]; -1 when none exists. Binary search over the sorted
        // breaks list keeps splitting ~O(n log n) instead of O(breaks × windows).
        int FloorBreak(int start, int ceiling) {
            var index = breaks.BinarySearch(ceiling);
            if (index < 0) {
                index = ~index - 1; // largest break strictly less than ceiling (~index is the first break above it)
            }
            // breaks[index] is now the largest break <= ceiling (index is -1 when every break exceeds ceiling).
            return index >= 0 && breaks[index] > start ? breaks[index] : -1;
        }

    }

    private const int CharsPerToken = 4;

    /// <summary>
    /// Approximate token count (~4 chars per token), used only to populate the observability
    /// <see cref="DocumentChunk.TokenCount"/> field — sizing is character-based and does not use this. A precise
    /// tokenizer can replace this without changing the contract.
    /// </summary>
    private static int EstimateTokens(string text) => Math.Max(1, (int)Math.Ceiling((double)text.Length / CharsPerToken));

    private static string Sha256Hex(string input) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

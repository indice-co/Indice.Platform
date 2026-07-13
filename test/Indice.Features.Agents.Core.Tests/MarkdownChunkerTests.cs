using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Workflows;

namespace Indice.Features.Agents.Core.Tests;

public class MarkdownChunkerTests
{
    private static AgentsOptions.IngestionOptions Options(int target = 800, int overlap = 100) =>
        new() { ChunkTargetTokens = target, ChunkOverlapTokens = overlap };

    [Fact]
    public void BuildsHeadingBreadcrumbsFromNestedHeadings() {
        var body = """
            # Guide

            Intro line.

            ## Install

            Install here.

            ## Usage

            Usage here.
            """;

        var chunks = MarkdownChunker.Chunk(body, "guide-doc", Options());

        Assert.Equal(3, chunks.Count);
        Assert.Equal(["Guide", "Guide > Install", "Guide > Usage"], chunks.Select(c => c.HeadingPath));
        Assert.Equal(["Guide", "Install", "Usage"], chunks.Select(c => c.Title));
        Assert.Equal([0, 1, 2], chunks.Select(c => c.ChunkIndex));
    }

    [Fact]
    public void SplitsOversizedSectionIntoMultipleOverlappingChunks() {
        // Each paragraph is ~33 chars (~9 tokens). target 18 (~2 paragraphs), overlap 9 (~1 paragraph).
        var body = """
            # Doc

            MARKER_A alpha alpha alpha alpha.

            MARKER_B bravo bravo bravo bravo.

            MARKER_C charlie charlie charlie charlie.

            MARKER_D delta delta delta delta.
            """;
        var markers = new[] { "MARKER_A", "MARKER_B", "MARKER_C", "MARKER_D" };

        var chunks = MarkdownChunker.Chunk(body, "doc", Options(target: 18, overlap: 9));

        Assert.True(chunks.Count >= 2, $"expected split, got {chunks.Count}");
        Assert.All(chunks, c => Assert.Equal("Doc", c.HeadingPath));
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.ChunkIndex));
        // Every paragraph is covered somewhere.
        Assert.All(markers, m => Assert.Contains(chunks, c => c.Content.Contains(m)));
        // Overlap: at least one paragraph is carried into an adjacent chunk.
        Assert.True(markers.Any(m => chunks.Count(c => c.Content.Contains(m)) >= 2),
            "expected overlap to duplicate at least one paragraph across chunks");
        // Bounded: no chunk wildly exceeds the target.
        Assert.All(chunks, c => Assert.True(c.TokenCount <= 18 * 2, $"chunk too big: {c.TokenCount} tokens"));
    }

    [Fact]
    public void FlatFileWithNoHeadingsDegradesToTokenWindowsAnchoredToDocumentTitle() {
        // A docx/pdf->md dump: one wall of text, no headings, no blank lines.
        var body = "This is a converted document with no markdown headings at all just flat prose "
                 + "that runs on and on describing the system and its behavior across many clauses "
                 + "until it is clearly longer than the configured chunk target size and must split.";

        var chunks = MarkdownChunker.Chunk(body, "converted-report", Options(target: 20, overlap: 5));

        Assert.True(chunks.Count >= 2, $"expected token-window split, got {chunks.Count}");
        Assert.All(chunks, c => Assert.Equal("converted-report", c.HeadingPath));
        Assert.All(chunks, c => Assert.Equal("converted-report", c.Title));
        Assert.All(chunks, c => Assert.True(c.TokenCount <= 20 * 2, $"chunk too big: {c.TokenCount} tokens"));
        Assert.Contains(chunks, c => c.Content.Contains("This is a converted document"));
        Assert.Contains(chunks, c => c.Content.Contains("must split."));
    }

    [Fact]
    public void PreservesCodeFenceVerbatimAndDoesNotSplitOnHashLinesInsideIt() {
        var body = """
            # API

            Use it like this:

            ```csharp
            # this looks like a heading but is inside code
            var x = 1;
            ```

            Done.
            """;

        var chunks = MarkdownChunker.Chunk(body, "api-doc", Options());

        // The '#' line inside the fence must NOT open a new section.
        Assert.Single(chunks);
        Assert.Equal("API", chunks[0].HeadingPath);
        Assert.Contains("```csharp", chunks[0].Content);
        Assert.Contains("# this looks like a heading but is inside code", chunks[0].Content);
        Assert.Contains("var x = 1;", chunks[0].Content);
    }

    [Fact]
    public void ExtractsCleanTextFromSetextHeadings() {
        // pandoc/docx->md often emit setext (underlined) headings instead of '#'.
        var body = """
            Title Line
            ==========

            Some content here.

            Sub Heading
            -----------

            More content.
            """;

        var chunks = MarkdownChunker.Chunk(body, "doc", Options());

        Assert.Equal(["Title Line", "Title Line > Sub Heading"], chunks.Select(c => c.HeadingPath));
        Assert.Equal(["Title Line", "Sub Heading"], chunks.Select(c => c.Title));
        // The '===' / '---' underline must not leak into breadcrumb or embedded content.
        Assert.All(chunks, c => Assert.DoesNotContain('=', c.Content));
    }

    [Fact]
    public void StripsInlineFormattingFromHeadingText() {
        var body = """
            ## Install **now** with `setup`

            Body.
            """;

        var chunks = MarkdownChunker.Chunk(body, "doc", Options());

        Assert.Equal("Install now with setup", chunks[0].HeadingPath);
    }

    [Fact]
    public void ExcludesYamlFrontMatterFromChunks() {
        var body = """
            ---
            title: Secret Title
            category: policy
            ---

            # Real Heading

            Body text here.
            """;

        var chunks = MarkdownChunker.Chunk(body, "fm-doc", Options());

        Assert.All(chunks, c => Assert.DoesNotContain("Secret Title", c.Content));
        Assert.All(chunks, c => Assert.DoesNotContain("category: policy", c.Content));
        Assert.Contains(chunks, c => c.HeadingPath == "Real Heading" && c.Content.Contains("Body text here."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n  \t ")]
    public void EmptyOrWhitespaceBodyProducesNoChunks(string body) {
        var chunks = MarkdownChunker.Chunk(body, "empty-doc", Options());
        Assert.Empty(chunks);
    }
}

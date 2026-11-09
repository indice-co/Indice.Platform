using System.Text;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Ingestion;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

public class MarkdownIngestionPipelineTests
{
    [Fact]
    public async Task IngestingMarkdownTypeRoutesThroughStructuralChunker() {
        var body = """
            Intro prose before any heading.

            # Guide

            ## Install

            Install steps.
            """;
        var store = new CapturingDocumentsService();
        var options = Microsoft.Extensions.Options.Options.Create(new AgentsOptions {
            Taxonomy = new AgentsOptions.TaxonomyOptions { Categories = ["general"], Languages = ["en"] },
        });
        var pipeline = new IngestionPipeline(store, options, new StubEmbeddingGenerator());

        var report = await pipeline.IngestAsync(Request(body, DocumentType.Markdown), CancellationToken.None);

        Assert.False(report.Skipped);
        Assert.Equal(store.Captured.Count, report.ChunksCreated);
        // Structural chunker ran: intro prose above the first heading is captured (the FAQ parser discards it),
        // a nested heading breadcrumb is produced, and content is NOT in the FAQ "Q:/A:" format.
        Assert.Contains(store.Captured, c => c.Chunk.Content.Contains("Intro prose before any heading."));
        Assert.Contains(store.Captured, c => c.Chunk.HeadingPath == "Guide > Install");
        Assert.All(store.Captured, c => Assert.DoesNotContain("Q: ", c.Chunk.Content));
    }

    private static IngestRequest Request(string body, DocumentType type) => new() {
        DocumentType = type,
        OpenMarkdownSourceStream = () => new MemoryStream(Encoding.UTF8.GetBytes(body)),
        Source = "local://test.md",
        FileName = "test.md",
        ContentType = "text/markdown",
        ContentLength = body.Length,
        Category = "general",
        Language = "en",
        IsPrivate = false,
    };

    private sealed class CapturingDocumentsService : IDocumentsService
    {
        public List<EmbeddedChunk> Captured { get; } = [];

        public Task<SourceDocument?> FindBySourceAsync(string source, bool includeData, CancellationToken cancellationToken) =>
            Task.FromResult<SourceDocument?>(null);

        public Task<Guid> ReplaceAsync(Guid? existingDocumentId, IngestedDocument document, IReadOnlyList<EmbeddedChunk> chunks, CancellationToken cancellationToken) {
            Captured.AddRange(chunks);
            return Task.FromResult(new Guid("11111111-1111-1111-1111-111111111111"));
        }

        public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(ReadOnlyMemory<float> queryVector, RetrievalFilters filters, int topK, double minScore, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ClearAsync() => throw new NotSupportedException();
    }

    private sealed class StubEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) {
            var embeddings = values.Select(_ => new Embedding<float>(new float[] { 0f, 0f, 0f })).ToList();
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}

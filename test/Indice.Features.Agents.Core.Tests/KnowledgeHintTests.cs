using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Ingestion;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

/// <summary>
/// "Triggered when the context fits": the router asks the knowledge base which agent's knowledge a message is about. The
/// guarantees that matter are that a restricted category is only ever searched for the agents the caller may use, and that
/// a hint is only a hint (it can be absent, wrong or failing without breaking routing).
/// </summary>
public class KnowledgeHintServiceTests
{
    private const string Restricted = "antifraud-kb";

    private static AgentInfo Agent(string name, params string[] categories) =>
        new(name, $"{name} agent", ["text/plain"], ["text/markdown"]) { KnowledgeCategories = categories };

    private static AgentsOptions Options(bool enabled = true) {
        var options = new AgentsOptions();
        options.Taxonomy.Categories = ["faq", "policy", Restricted];
        options.Taxonomy.RestrictedCategories = [Restricted];
        options.Routing.KnowledgeHints.Enabled = enabled;
        options.Routing.KnowledgeHints.TopK = 7;
        options.Routing.KnowledgeHints.MinScore = 0.4;
        return options;
    }

    private static RetrievedChunk Chunk(string? category, double score) =>
        new() { Id = Guid.NewGuid(), Category = category, Score = score, Source = new SourceDocumentLink { SourceTitle = "doc" } };

    private static (KnowledgeHintService Service, FakeDocuments Documents, CountingEmbeddings Embeddings) Create(AgentsOptions options, params RetrievedChunk[] found) {
        var documents = new FakeDocuments(found);
        var embeddings = new CountingEmbeddings();
        return (new KnowledgeHintService(embeddings, documents, Microsoft.Extensions.Options.Options.Create(options), NullLogger<KnowledgeHintService>.Instance), documents, embeddings);
    }

    [Fact]
    public async Task Hints_Off_Costs_Nothing() {
        var (service, documents, embeddings) = Create(Options(enabled: false), Chunk(Restricted, 0.9));

        var hints = await service.GetHintsAsync("anything", [Agent("risk", Restricted)], TestContext.Current.CancellationToken);

        Assert.Empty(hints);
        Assert.Equal(0, embeddings.Calls);
        Assert.Equal(0, documents.Searches);
    }

    [Fact]
    public async Task Without_Any_Declared_Knowledge_There_Is_Nothing_To_Search_For() {
        var (service, documents, embeddings) = Create(Options(), Chunk("faq", 0.9));

        var hints = await service.GetHintsAsync("anything", [Agent("knowledge")], TestContext.Current.CancellationToken);

        Assert.Empty(hints);
        Assert.Equal(0, embeddings.Calls);
        Assert.Equal(0, documents.Searches);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_Blank_Message_Is_Not_Embedded(string message) {
        var (service, _, embeddings) = Create(Options(), Chunk(Restricted, 0.9));

        var hints = await service.GetHintsAsync(message, [Agent("risk", Restricted)], TestContext.Current.CancellationToken);

        Assert.Empty(hints);
        Assert.Equal(0, embeddings.Calls);
    }

    [Fact]
    public async Task A_Restricted_Declaration_Opts_The_Search_In_And_Groups_Hits_By_Category() {
        var (service, documents, _) = Create(Options(),
            Chunk(Restricted, 0.55), Chunk(Restricted, 0.71), Chunk(Restricted, 0.62),
            Chunk("faq", 0.99));   // a public category nobody declares: not a hint
        var agents = new[] { Agent("risk", Restricted), Agent("knowledge") };

        var hints = await service.GetHintsAsync("evidence pack for a transaction", agents, TestContext.Current.CancellationToken);

        var hint = Assert.Single(hints);
        Assert.Equal(Restricted, hint.Category);
        Assert.Equal(0.71, hint.Score);
        Assert.Equal(new[] { "risk" }, hint.AgentNames);
        Assert.True(documents.LastFilters!.IncludeRestricted);
        Assert.Null(documents.LastFilters.Category);
        Assert.Equal(7, documents.LastTopK);
        Assert.Equal(0.4, documents.LastMinScore);
    }

    [Fact]
    public async Task Declaring_Only_An_Ordinary_Category_Never_Opts_In_To_Restricted_Knowledge() {
        var (service, documents, _) = Create(Options(), Chunk("policy", 0.8));

        var hints = await service.GetHintsAsync("holiday policy", [Agent("hr", "policy")], TestContext.Current.CancellationToken);

        Assert.Equal("policy", Assert.Single(hints).Category);
        Assert.False(documents.LastFilters!.IncludeRestricted);
    }

    [Fact]
    public async Task Chunks_Of_A_Category_No_Allowed_Agent_Declares_Are_Ignored_Even_If_The_Search_Returned_Them() {
        // The caller may not use the risk agent, so it is not in the allowed list. Whatever the search returns for its
        // category must not surface as a hint.
        var (service, _, _) = Create(Options(), Chunk(Restricted, 0.95), Chunk("policy", 0.5));

        var hints = await service.GetHintsAsync("evidence pack", [Agent("hr", "policy")], TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "policy" }, hints.Select(h => h.Category));
    }

    [Fact]
    public async Task Category_Matching_Is_Case_Insensitive_And_Hints_Are_Ordered_Best_First() {
        var (service, _, _) = Create(Options(), Chunk("POLICY", 0.45), Chunk("Antifraud-KB", 0.8), Chunk(null, 0.99));
        var agents = new[] { Agent("hr", "policy"), Agent("risk", Restricted), Agent("audit", "antifraud-kb") };

        var hints = await service.GetHintsAsync("question", agents, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { Restricted, "policy" }, hints.Select(h => h.Category));
        Assert.Equal(new[] { "risk", "audit" }, hints[0].AgentNames);
    }

    // --- test doubles -------------------------------------------------------------------------------------------------

    private sealed class FakeDocuments(IReadOnlyList<RetrievedChunk> found) : IDocumentsService
    {
        public int Searches { get; private set; }
        public RetrievalFilters? LastFilters { get; private set; }
        public int LastTopK { get; private set; }
        public double LastMinScore { get; private set; }

        public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(ReadOnlyMemory<float> queryVector, RetrievalFilters filters, int topK, double minScore, CancellationToken cancellationToken) {
            Searches++;
            LastFilters = filters;
            LastTopK = topK;
            LastMinScore = minScore;
            return Task.FromResult(found);
        }

        public Task<SourceDocument?> FindBySourceAsync(string source, bool includeData, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid> ReplaceAsync(Guid? existingDocumentId, IngestedDocument document, IReadOnlyList<EmbeddedChunk> chunks, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeSnippet>> FindByHeadingAsync(string category, IReadOnlyCollection<string> headingPrefixes, string? language, bool includeRestricted, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ClearAsync() => throw new NotSupportedException();
    }

    private sealed class CountingEmbeddings : IEmbeddingGenerator<string, Embedding<float>>
    {
        public int Calls { get; private set; }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) {
            Calls++;
            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(values.Select(_ => new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f })).ToList()));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}

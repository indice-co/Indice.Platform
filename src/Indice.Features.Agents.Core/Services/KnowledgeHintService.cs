using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc/>
public sealed class KnowledgeHintService : IKnowledgeHintService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly IDocumentsService _documents;
    private readonly AgentsOptions _options;
    private readonly ILogger<KnowledgeHintService> _logger;

    /// <summary>Creates a new <see cref="KnowledgeHintService"/>.</summary>
    public KnowledgeHintService(IEmbeddingGenerator<string, Embedding<float>> embeddings, IDocumentsService documents, IOptions<AgentsOptions> options, ILogger<KnowledgeHintService> logger) {
        _embeddings = embeddings;
        _documents = documents;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<KnowledgeHint>> GetHintsAsync(string message, IReadOnlyList<AgentInfo> allowedAgents, CancellationToken cancellationToken = default) {
        var hints = _options.Routing.KnowledgeHints;
        if (!hints.Enabled || string.IsNullOrWhiteSpace(message)) {
            return [];
        }
        // category -> the agents that declare it. Built only from agents the caller may use, which is what keeps a restricted
        // category out of reach for everyone else.
        var owners = allowedAgents
            .SelectMany(agent => (agent.KnowledgeCategories ?? []).Where(category => !string.IsNullOrWhiteSpace(category)).Select(category => (Category: category.Trim(), agent.Name)))
            .GroupBy(pair => pair.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)[.. group.Select(pair => pair.Name).Distinct(StringComparer.OrdinalIgnoreCase)], StringComparer.OrdinalIgnoreCase);
        if (owners.Count == 0) {
            return [];
        }

        var vector = await _embeddings.GenerateVectorAsync(message, cancellationToken: cancellationToken);
        var chunks = await _documents.SearchAsync(
            vector,
            new RetrievalFilters { IncludeRestricted = owners.Keys.Any(_options.Taxonomy.IsRestricted) },
            hints.TopK,
            hints.MinScore,
            cancellationToken);

        var found = chunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk.Category) && owners.ContainsKey(chunk.Category!))
            .GroupBy(chunk => owners.Keys.First(category => string.Equals(category, chunk.Category, StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase)
            .Select(group => new KnowledgeHint(group.Key, group.Max(chunk => chunk.Score), owners[group.Key]))
            .OrderByDescending(hint => hint.Score)
            .ToList();
        _logger.LogDebug("Knowledge hints for routing: {Hints}", found.Select(hint => $"{hint.Category}={hint.Score:0.00}"));
        return found;
    }
}

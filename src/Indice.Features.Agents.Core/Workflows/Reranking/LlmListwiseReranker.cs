using System.Text;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Workflows.Reranking;

/// <summary>
/// Default <see cref="ILlmReranker"/> implementation: scores all candidates in a single fast-model call,
/// returns the top-N by descending score. Falls back to input order on any LLM/parse failure.
/// </summary>
public class LlmListwiseReranker : ILlmReranker
{
    private readonly AIAgent _agent;
    private readonly int _snippetLength;

    /// <summary>Creates a new <see cref="LlmListwiseReranker"/>.</summary>
    public LlmListwiseReranker(AzureOpenAIClient openAIClient, IOptions<AgentsOptions> options, IPromptTemplateRenderer prompts) {
        var opts = options.Value;
        _snippetLength = opts.Retrieval.RerankSnippetLength;
        _agent = openAIClient
            .GetChatClient(opts.AzureOpenAI.Deployments.Fast!)
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = new ChatOptions {
                    Temperature = opts.Models.Fast.Temperature,
                    MaxOutputTokens = opts.Models.Fast.MaxOutputTokens,
                    Instructions = prompts.Render("Reranker"),
                },
                Name = "DexReranker",
            });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RetrievedChunk>> RerankAsync(string question, IReadOnlyList<RetrievedChunk> candidates,
        int topN, CancellationToken cancellationToken) {
        if (candidates.Count == 0) {
            return Array.Empty<RetrievedChunk>();
        }
        var prompt = BuildPrompt(question, candidates, _snippetLength);
        var response = await _agent.RunAsync<RerankScores>(prompt, cancellationToken: cancellationToken);
        var scoreByIndex = response.Result.Scores
            .Where(s => s.Index >= 0 && s.Index < candidates.Count)
            .ToDictionary(s => s.Index, s => s.Score);
        var reranked = candidates
            .Select((c, i) => new RetrievedChunk {
                ChunkId = c.ChunkId,
                DocumentId = c.DocumentId,
                Title = c.Title,
                HeadingPath = c.HeadingPath,
                Content = c.Content,
                TokenCount = c.TokenCount,
                Score = scoreByIndex.TryGetValue(i, out var s) ? s : c.Score,
            })
            .OrderByDescending(c => c.Score)
            .Take(topN)
            .ToList();
        return reranked;
    }

    private static string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> candidates, int snippetLength) {
        var sb = new StringBuilder();
        sb.Append("Question: ").AppendLine(question).AppendLine();
        sb.AppendLine("Candidates:");
        for (var i = 0; i < candidates.Count; i++) {
            var c = candidates[i];
            var snippet = c.Content.Length > snippetLength ? c.Content[..snippetLength] + "…" : c.Content;
            sb.Append('[').Append(i).Append("] ");
            if (!string.IsNullOrWhiteSpace(c.Title)) {
                sb.Append(c.Title).Append(" — ");
            }
            sb.AppendLine(snippet);
        }
        return sb.ToString();
    }

    private sealed class RerankScores
    {
        public List<RerankScore> Scores { get; set; } = new();
    }

    private sealed class RerankScore
    {
        public int Index { get; set; }
        public double Score { get; set; }
    }
}

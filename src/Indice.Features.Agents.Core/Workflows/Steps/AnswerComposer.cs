using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Composes the final grounded answer from the reranked candidates. Instructs the model to ground only
/// in the provided context and cite chunk IDs in <c>[#chunkId]</c> form. Projects the candidates into
/// <see cref="Models.Citation"/> records on the Output payload.
/// </summary>
public sealed class AnswerComposer : Executor<RerankOutput, GroundedAnswerOutput>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="AnswerComposer"/>.</summary>
    public AnswerComposer([FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, 
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        ConversationStoreChatHistoryProvider historyProvider) : base("AnswerComposer") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;

        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("AnswerComposer", new {
            strictGrounding = _options.Pipeline.StrictGrounding,
        });
        _agent = chatClient.AsAIAgent(
                options: new ChatClientAgentOptions() {
                    ChatOptions = chatOptions,
                    AIContextProviders = [userClaimsProvider],
                    Name = "DexAnswerComposer",
                    ChatHistoryProvider = historyProvider,
                    // Chat completions is stateless; the M.E.AI OpenAI client echoes the request ConversationId onto
                    // the response, which the agent would otherwise misread as server-side history and throw.
                    ThrowOnChatHistoryProviderConflict = false,
                });
    }

    /// <inheritdoc/>
    public override async ValueTask<GroundedAnswerOutput> HandleAsync(RerankOutput message,
        IWorkflowContext context, CancellationToken cancellationToken = default) {
        var state = await context.GetConversationStateAsync(cancellationToken);
        var candidates = message.RerankedCandidates;
        var prompt = BuildPrompt(state.Message.Text, candidates);
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        ConversationStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(state.ConversationId));

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text. Token
        // usage arrives as trailing UsageContent on the final update(s); fold it and report it once.
        var answer = new StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(prompt, agentSession, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
            }
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, update), cancellationToken);
        }

        var citations = candidates
            .Select((c, index) => new Models.Citation {
                ChunkId = c.Id,
                DocumentId = c.Source.Id,
                Title = c.Title,
                SourceUrl = c.Source.SourceUrl,
                HeadingPath = c.HeadingPath,
                Score = c.Score,
                Number = index + 1,
            })
            .ToList();
        // Citation markers arrive fragmented across deltas, so annotations cannot be stamped per-update.
        // Instead, emit them once in a trailing update now that the full text is known and offsets are exact.
        var annotations = BuildCitationAnnotations(answer.ToString(), citations);
        if (annotations.Count > 0) {
            var annotationUpdate = new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(string.Empty) { Annotations = annotations }]);
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, annotationUpdate), cancellationToken);
        }
        var sources = candidates.Select(c => c.Source).DistinctBy(x => x.Id).ToList();
        return new GroundedAnswerOutput(
            answer.ToString(),
            citations,
            sources
        );
    }

    /// <summary>
    /// Maps footnote markers in the final answer to <see cref="CitationAnnotation"/> instances, one per marker,
    /// with exact <see cref="TextSpanAnnotatedRegion"/> offsets. Handles both formats the model emits inside a
    /// <c>&lt;sup&gt;</c> group: bare positional markers (<c>&lt;sup&gt;[1][2][5]&lt;/sup&gt;</c>) joined on
    /// <see cref="Models.Citation.Number"/>, and linked markers (<c>&lt;sup&gt;[5](#chunkId)&lt;/sup&gt;</c>)
    /// joined on <see cref="Models.Citation.ChunkId"/> with positional fallback. Unresolved markers are skipped.
    /// </summary>
    private static List<AIAnnotation> BuildCitationAnnotations(string answer, IReadOnlyList<Models.Citation> citations) {
        var annotations = new List<AIAnnotation>();
        if (string.IsNullOrEmpty(answer) || citations.Count == 0) {
            return annotations;
        }
        var citationsByNumber = citations.ToDictionary(c => c.Number);
        var citationsByChunkId = citations.ToDictionary(c => c.ChunkId);
        // Outer match: a whole <sup>...</sup> group (tolerates a malformed closing tag like </su>).
        // Inner match: each [n] token with an optional (#chunkId) markdown link target.
        foreach (var inner in Regex.Matches(answer, @"<sup>(.*?)</su\w*>", RegexOptions.Singleline)
                                 .Cast<Match>()
                                 .Select(group => group.Groups[1])) {
            foreach (Match marker in Regex.Matches(inner.Value, @"\[(\d+)\](?:\(#([0-9a-fA-F\-]{36})\))?")) {
                Models.Citation? citation = null;
                if (marker.Groups[2].Success && Guid.TryParse(marker.Groups[2].Value, out var chunkId)) {
                    citationsByChunkId.TryGetValue(chunkId, out citation);
                }
                if (citation is null && int.TryParse(marker.Groups[1].Value, out var number)) {
                    citationsByNumber.TryGetValue(number, out citation);
                }
                if (citation is null) {
                    continue;
                }
                // Annotate the whole marker ([n] or [n](#chunkId)) at its absolute position in the answer.
                var start = inner.Index + marker.Index;
                var end = start + marker.Length;
                annotations.Add(new CitationAnnotation {
                    Title = citation.Title,
                    Url = Uri.TryCreate(citation.SourceUrl, UriKind.Absolute, out var url) ? url : null,
                    FileId = citation.ChunkId.ToString(),
                    Snippet = citation.Snippet,
                    AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = start, EndIndex = end }],
                    RawRepresentation = citation
                });
            }
        }
        return annotations;
    }

    private static string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> candidates) {
        var sb = new StringBuilder();
        sb.AppendLine("CONTEXT:");
        if (candidates.Count == 0) {
            sb.AppendLine("(no candidates retrieved)");
        }
        for (var i = 0; i < candidates.Count; i++) {
            var c = candidates[i];
            sb.Append($"[{i + 1}](#{c.Id}) ");
            if (!string.IsNullOrWhiteSpace(c.Title)) {
                sb.Append(c.Title).Append(" — ");
            }
            sb.AppendLine(c.Content);
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.Append("QUESTION: ").AppendLine(question);
        return sb.ToString();
    }
}

/// <summary>
/// The canonical Output of the last step of a Dex RAG pipeline. Carries the grounded answer and the
/// citations the answer was grounded against.
/// </summary>
/// <param name="Answer">The final answer composed by the model, or <c>null</c> if the model was unable to produce a grounded answer.</param>
/// <param name="Citations">The citations the answer was grounded against, projected from the reranked candidates.</param>
/// <param name="Sources">Links to the source documents that were retrieved and used to compose the answer; empty for out-of-scope responses and on error.</param>
public record GroundedAnswerOutput(string? Answer, IReadOnlyList<Citation> Citations, IReadOnlyList<SourceDocumentLink> Sources);

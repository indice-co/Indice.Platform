using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Asks the system-one decision model (Microsoft-Decision-1) bounded, typed questions about the latest user message.
/// The caller only declares the questions; this service loads the conversation history, adds the caller's context
/// prompt, sends the request, scores each answer against <c>Dex:SystemOne:MinConfidence</c> and returns the decision —
/// or <c>null</c> when the feature is off, the call fails or a required answer is unsure, so the caller falls back to its LLM path.
/// </summary>
/// <remarks>See <see href="https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-microsoft-decision"/>.</remarks>
public class SystemOneDecisionService
{
    private const string DecisionPath = "providers/microsoft/v1/systemone";
    private static readonly ConcurrentDictionary<string, bool> _missingContextPrompts = new();

    private readonly HttpClient _httpClient;
    private readonly AgentsOptions _options;
    private readonly IConversationStore _store;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly ILogger<SystemOneDecisionService> _logger;

    /// <summary>Creates a new <see cref="SystemOneDecisionService"/>.</summary>
    public SystemOneDecisionService(HttpClient httpClient, IOptions<AgentsOptions> options, IConversationStore store, IPromptTemplateRenderer prompts,
        ILogger<SystemOneDecisionService> logger) {
        _httpClient = httpClient;
        _options = options.Value;
        _store = store;
        _prompts = prompts;
        _logger = logger;
    }

    /// <summary>Whether a system-one deployment is configured (<c>Dex:AzureOpenAI:Deployments:SystemOne</c>).</summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Deployments.SystemOne);

    /// <summary>Asks the system-one model the <paramref name="request"/>'s questions.</summary>
    /// <param name="request">The latest message, its conversation and the questions to answer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision, or <c>null</c> when the caller should fall back to its LLM path.</returns>
    public async Task<SystemOneDecision?> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsEnabled || request.Questions.Count == 0) {
            return null;
        }
        try {
            var body = new JsonObject {
                ["model"] = _options.AzureOpenAI.Deployments.SystemOne,
                ["state"] = await BuildStateAsync(request, cancellationToken),
                ["questions"] = BuildQuestions(request.Questions),
            };
            var endpoint = (_options.AzureOpenAI.SystemOneEndpoint ?? _options.AzureOpenAI.Endpoint)!.TrimEnd('/');
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/{DecisionPath}") {
                // Buffered on purpose: the decision API rejects chunked bodies (400 no_content_length_header).
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("api-key", _options.AzureOpenAI.SystemOneApiKey ?? _options.AzureOpenAI.ApiKey);
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) {
                _logger.LogWarning("System-one decision failed with {StatusCode}: {Payload}", (int)response.StatusCode, payload);
                return null;
            }
            return ReadDecision(request, JsonNode.Parse(payload));
        } catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
            _logger.LogWarning(exception, "System-one decision failed; falling back to the LLM.");
            return null;
        }
    }

    private async Task<JsonObject> BuildStateAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken) {
        var state = new JsonObject();
        var context = RenderContext(request.ContextPrompt);
        if (!string.IsNullOrWhiteSpace(context)) {
            state["context"] = context.Trim();
        }
        if (request.ConversationId is { } conversationId && conversationId != Guid.Empty) {
            var history = new JsonArray();
            foreach (var message in await _store.GetHistoryAsync(conversationId, cancellationToken)) {
                var text = message.Text;
                if (string.IsNullOrWhiteSpace(text)) {
                    continue;
                }
                history.Add(new JsonObject {
                    ["role"] = message.Role.Value,
                    ["text"] = text,
                });
            }
            if (history.Count > 0) {
                state["history"] = history;
            }
        }
        state["message"] = request.Message;
        return state;
    }

    private string? RenderContext(string? promptName) {
        if (string.IsNullOrWhiteSpace(promptName) || _missingContextPrompts.ContainsKey(promptName)) {
            return null;
        }
        try {
            return _prompts.Render(promptName);
        } catch (InvalidOperationException exception) {
            _missingContextPrompts.TryAdd(promptName, true);
            _logger.LogDebug(exception, "System-one context prompt '{Prompt}' not found; deciding without context.", promptName);
            return null;
        }
    }

    private static JsonObject BuildQuestions(Dictionary<string, SystemOneQuestion> questions) {
        var result = new JsonObject();
        foreach (var (name, question) in questions) {
            var node = new JsonObject {
                ["type"] = question.Type == SystemOneQuestionType.Choice ? "choice" : "noul",
                ["instructions"] = question.Instructions,
            };
            if (question.Criteria is { Count: > 0 }) {
                var criteria = new JsonObject();
                foreach (var (option, description) in question.Criteria) {
                    criteria[option] = description;
                }
                node["criteria"] = criteria;
            }
            result[name] = node;
        }
        return result;
    }

    private SystemOneDecision? ReadDecision(SystemOneDecisionRequest request, JsonNode? payload) {
        var answers = new Dictionary<string, SystemOneAnswer>();
        var answersNode = payload?["answers"] as JsonObject;
        foreach (var (name, question) in request.Questions) {
            var answer = answersNode?[name] is JsonObject node ? ReadAnswer(question, node) : null;
            if (answer is not null) {
                answers[name] = answer;
            }
            if (question.Required && answer?.IsConfident != true) {
                _logger.LogDebug("System-one is unsure about '{Question}' (choice {Choice}, confidence {Confidence}); falling back to the LLM.",
                    name, answer?.Choice ?? answer?.Probability?.ToString(), answer?.Confidence);
                return null;
            }
        }
        var usage = payload?["usage"];
        return new SystemOneDecision {
            Answers = answers,
            Usage = usage is null ? null : new UsageDetails {
                InputTokenCount = usage["input_tokens"]?.GetValue<long>(),
                OutputTokenCount = usage["output_tokens"]?.GetValue<long>(),
                TotalTokenCount = (usage["input_tokens"]?.GetValue<long>() ?? 0) + (usage["output_tokens"]?.GetValue<long>() ?? 0),
            },
        };
    }

    private SystemOneAnswer? ReadAnswer(SystemOneQuestion question, JsonObject node) {
        var threshold = question.MinConfidence ?? _options.SystemOne.MinConfidence;
        if (question.Type == SystemOneQuestionType.Noul) {
            if (node["noul"] is not JsonValue value || !value.TryGetValue<double>(out var probability)) {
                return null;
            }
            var confidence = Math.Max(probability, 1 - probability);
            return new SystemOneAnswer {
                Type = SystemOneQuestionType.Noul,
                Probability = probability,
                Confidence = confidence,
                IsConfident = confidence >= threshold,
            };
        }
        var choice = node["choice"]?.GetValue<string>();
        var probabilities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (node["probabilities"] is JsonObject probabilitiesNode) {
            foreach (var (option, probabilityNode) in probabilitiesNode) {
                if (probabilityNode is JsonValue probabilityValue && probabilityValue.TryGetValue<double>(out var optionProbability)) {
                    probabilities[option] = optionProbability;
                }
            }
        }
        var choiceConfidence = choice is not null && probabilities.TryGetValue(choice, out var chosen)
            ? chosen
            : node["confidence"] is JsonValue confidenceValue && confidenceValue.TryGetValue<double>(out var reported) ? reported : 0;
        // Map the answer back to the option exactly as the caller spelled it; anything else is not an answer.
        var chosenOption = question.Criteria?.Keys.FirstOrDefault(key => string.Equals(key, choice, StringComparison.OrdinalIgnoreCase));
        return new SystemOneAnswer {
            Type = SystemOneQuestionType.Choice,
            Choice = chosenOption ?? choice,
            Probabilities = probabilities,
            Confidence = choiceConfidence,
            IsConfident = chosenOption is not null && choiceConfidence >= threshold,
        };
    }
}

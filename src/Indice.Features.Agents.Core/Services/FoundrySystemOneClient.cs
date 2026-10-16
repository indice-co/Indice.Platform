using System.Text;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// <see cref="ISystemOneClient"/> for a Microsoft-Decision-1 deployment on a Microsoft Foundry resource
/// (<c>POST {endpoint}/providers/microsoft/v1/systemone</c> with an <c>api-key</c> header).
/// </summary>
/// <remarks>See <see href="https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-microsoft-decision"/>.</remarks>
public sealed class FoundrySystemOneClient : ISystemOneClient
{
    private const string DecisionPath = "providers/microsoft/v1/systemone";

    private readonly HttpClient _httpClient;
    private readonly Uri _decisionUri;
    private readonly string _apiKey;
    private readonly string _deploymentName;

    /// <summary>Creates a new <see cref="FoundrySystemOneClient"/>.</summary>
    /// <param name="httpClient">The HTTP client that sends the requests.</param>
    /// <param name="endpoint">The Foundry resource endpoint, e.g. <c>https://my-resource.services.ai.azure.com</c>.</param>
    /// <param name="apiKey">The resource API key.</param>
    /// <param name="deploymentName">The decision model deployment name, e.g. <c>Decision-1</c>.</param>
    public FoundrySystemOneClient(HttpClient httpClient, Uri endpoint, string apiKey, string deploymentName) {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(endpoint);
        _httpClient = httpClient;
        _decisionUri = new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}/{DecisionPath}");
        _apiKey = apiKey;
        _deploymentName = deploymentName;
    }

    /// <inheritdoc/>
    public async Task<SystemOneDecision> DecideAsync(IEnumerable<ChatMessage> messages, SystemOneDecisionOptions options, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(options);
        var body = new JsonObject {
            ["model"] = _deploymentName,
            ["state"] = BuildState(messages, options.Instructions),
            ["questions"] = BuildQuestions(options.Questions),
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, _decisionUri) {
            // Buffered on purpose: the decision API rejects chunked bodies (400 no_content_length_header).
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"System-one decision failed with {(int)response.StatusCode}: {payload}", inner: null, response.StatusCode);
        }
        return ReadDecision(options.Questions, JsonNode.Parse(payload));
    }

    private static JsonObject BuildState(IEnumerable<ChatMessage> messages, string? instructions) {
        var turns = messages.Where(message => !string.IsNullOrWhiteSpace(message.Text)).ToList();
        if (turns.Count == 0) {
            throw new ArgumentException("At least one message with text is required.", nameof(messages));
        }
        var state = new JsonObject();
        if (!string.IsNullOrWhiteSpace(instructions)) {
            state["context"] = instructions.Trim();
        }
        if (turns.Count > 1) {
            var history = new JsonArray();
            foreach (var turn in turns.Take(turns.Count - 1)) {
                history.Add(new JsonObject {
                    ["role"] = turn.Role.Value,
                    ["text"] = turn.Text,
                });
            }
            state["history"] = history;
        }
        state["message"] = turns[^1].Text;
        return state;
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

    private static SystemOneDecision ReadDecision(Dictionary<string, SystemOneQuestion> questions, JsonNode? payload) {
        var answers = new Dictionary<string, SystemOneAnswer>();
        var answersNode = payload?["answers"] as JsonObject;
        foreach (var (name, question) in questions) {
            if (answersNode?[name] is JsonObject node && ReadAnswer(question, node) is { } answer) {
                answers[name] = answer;
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

    private static SystemOneAnswer? ReadAnswer(SystemOneQuestion question, JsonObject node) {
        if (question.Type == SystemOneQuestionType.Noul) {
            if (node["noul"] is not JsonValue value || !value.TryGetValue<double>(out var probability)) {
                return null;
            }
            return new SystemOneAnswer {
                Type = SystemOneQuestionType.Noul,
                Probability = probability,
                Confidence = Math.Max(probability, 1 - probability),
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
        // Map the answer back to the option exactly as the caller spelled it; anything else is not an answer.
        var chosenOption = question.Criteria?.Keys.FirstOrDefault(key => string.Equals(key, choice, StringComparison.OrdinalIgnoreCase));
        var confidence = chosenOption is null ? 0
            : probabilities.TryGetValue(chosenOption, out var chosen) ? chosen
            : node["confidence"] is JsonValue confidenceValue && confidenceValue.TryGetValue<double>(out var reported) ? reported : 0;
        return new SystemOneAnswer {
            Type = SystemOneQuestionType.Choice,
            Choice = chosenOption,
            Probabilities = probabilities,
            Confidence = confidence,
        };
    }
}

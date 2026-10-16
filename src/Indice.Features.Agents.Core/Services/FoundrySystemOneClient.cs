using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    // camelCase properties, "noul" / "choice" for the question type, no nulls; dictionary keys (question and option names) as given.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

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
    public async Task<SystemOneDecision> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        var body = new SystemOneDecisionRequest {
            Model = request.Model ?? _deploymentName,
            State = request.State,
            Questions = request.Questions,
        };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _decisionUri) {
            // Buffered on purpose: the decision API rejects chunked bodies (400 no_content_length_header).
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("api-key", _apiKey);
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) {
            throw new HttpRequestException($"System-one decision failed with {(int)response.StatusCode}: {payload}", inner: null, response.StatusCode);
        }
        return ToDecision(request.Questions, JsonSerializer.Deserialize<SystemOneDecisionResponse>(payload, JsonOptions));
    }

    private static SystemOneDecision ToDecision(Dictionary<string, SystemOneQuestion> questions, SystemOneDecisionResponse? response) {
        var answers = new Dictionary<string, SystemOneAnswer>();
        foreach (var (name, question) in questions) {
            if (response?.Answers?.GetValueOrDefault(name) is { } payload && ToAnswer(question, payload) is { } answer) {
                answers[name] = answer;
            }
        }
        var usage = response?.Usage;
        return new SystemOneDecision {
            Answers = answers,
            Usage = usage is null ? null : new UsageDetails {
                InputTokenCount = usage.InputTokens,
                OutputTokenCount = usage.OutputTokens,
                TotalTokenCount = (usage.InputTokens ?? 0) + (usage.OutputTokens ?? 0),
            },
        };
    }

    private static SystemOneAnswer? ToAnswer(SystemOneQuestion question, SystemOneAnswerPayload payload) {
        if (question.Type == SystemOneQuestionType.Noul) {
            return payload.Noul is not { } probability ? null : new SystemOneAnswer {
                Type = SystemOneQuestionType.Noul,
                Probability = probability,
                Confidence = Math.Max(probability, 1 - probability),
            };
        }
        var probabilities = new Dictionary<string, double>(payload.Probabilities ?? [], StringComparer.OrdinalIgnoreCase);
        // Map the answer back to the option exactly as the caller spelled it; anything else is not an answer.
        var chosenOption = question.Criteria?.Keys.FirstOrDefault(key => string.Equals(key, payload.Choice, StringComparison.OrdinalIgnoreCase));
        var confidence = chosenOption is null ? 0
            : probabilities.TryGetValue(chosenOption, out var chosen) ? chosen
            : payload.Confidence ?? 0;
        return new SystemOneAnswer {
            Type = SystemOneQuestionType.Choice,
            Choice = chosenOption,
            Probabilities = probabilities,
            Confidence = confidence,
        };
    }
}

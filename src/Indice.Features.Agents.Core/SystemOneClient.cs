using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services
{
    /// <summary>
    /// A client for a system-one decision model (e.g. Microsoft-Decision-1), which answers typed, bounded questions about a
    /// conversation with probabilities instead of generated text.
    /// </summary>
    public interface ISystemOneClient
    {
        /// <summary>Answers the <paramref name="request"/>'s questions about its state.</summary>
        /// <param name="request">The state and the questions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The answers, keyed by question name.</returns>
        /// <exception cref="HttpRequestException">The decision endpoint returned an unsuccessful status code.</exception>
        Task<SystemOneDecision> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default);
    }

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
        private readonly bool _enabled;

        /// <summary>Creates a new <see cref="FoundrySystemOneClient"/>.</summary>
        /// <param name="httpClient">The HTTP client that sends the requests.</param>
        /// <param name="endpoint">The Foundry resource endpoint, e.g. <c>https://my-resource.services.ai.azure.com</c>.</param>
        /// <param name="apiKey">The resource API key.</param>
        /// <param name="deploymentName">The decision model deployment name, e.g. <c>Decision-1</c>.</param>
        /// <param name="enabled">Whether the decision model is on; when <c>false</c> every decision comes back without answers and nothing is sent.</param>
        public FoundrySystemOneClient(HttpClient httpClient, Uri endpoint, string apiKey, string deploymentName, bool enabled) {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(endpoint);
            _httpClient = httpClient;
            _decisionUri = new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}/{DecisionPath}");
            _apiKey = apiKey;
            _deploymentName = deploymentName;
            _enabled = enabled;
        }

        /// <inheritdoc/>
        /// <remarks>When the client is disabled the decision has no answers, so a caller treats every question as unsure.</remarks>
        public async Task<SystemOneDecision> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default) {
            ArgumentNullException.ThrowIfNull(request);
            if (!_enabled) {
                return new SystemOneDecision();
            }
            var body = request with { Model = request.Model ?? _deploymentName };
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
            return JsonSerializer.Deserialize<SystemOneDecisionResponse>(payload, JsonOptions)?.ToDecision(request.Questions) ?? new SystemOneDecision();
        }
    }
}

namespace Indice.Features.Agents.Core.Models
{
    /// <summary>The kind of decision a <see cref="SystemOneQuestion"/> asks the system-one model for.</summary>
    public enum SystemOneQuestionType
    {
        /// <summary>A yes/no condition; the answer is the probability (0..1) that it holds.</summary>
        Noul = 0,

        /// <summary>One option out of a fixed set; the answer is the chosen option and the probability of each option.</summary>
        Choice = 1
    }

    /// <summary>A request to a system-one decision model: the state to judge and the typed questions to answer about it.</summary>
    public record SystemOneDecisionRequest
    {
        /// <summary>The decision model deployment, e.g. <c>Decision-1</c>. <c>null</c> uses the client's deployment.</summary>
        public string? Model { get; init; }

        /// <summary>What the questions are about.</summary>
        public SystemOneState State { get; init; } = new();

        /// <summary>The questions to answer, keyed by the name the answers come back under.</summary>
        public Dictionary<string, SystemOneQuestion> Questions { get; init; } = [];
    }

    /// <summary>The state a <see cref="SystemOneDecisionRequest"/> asks about: the latest message, the history before it and the context.</summary>
    public record SystemOneState
    {
        /// <summary>Background the model judges the message against; optional.</summary>
        public string? Context { get; init; }

        /// <summary>The earlier turns of the conversation, oldest first; optional.</summary>
        public List<SystemOneTurn>? History { get; init; }

        /// <summary>The message the questions are about.</summary>
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>One earlier turn of the conversation in a <see cref="SystemOneState"/>.</summary>
    public record SystemOneTurn
    {
        /// <summary>Who wrote it, e.g. <c>user</c> or <c>assistant</c>.</summary>
        public string Role { get; init; } = string.Empty;

        /// <summary>What was written.</summary>
        public string Text { get; init; } = string.Empty;
    }

    /// <summary>One typed question of a <see cref="SystemOneDecisionRequest"/>.</summary>
    public record SystemOneQuestion
    {
        /// <summary>The kind of decision asked for.</summary>
        public SystemOneQuestionType Type { get; init; }

        /// <summary>The question itself, e.g. <c>Which agent should handle the latest message?</c>.</summary>
        public string Instructions { get; init; } = string.Empty;

        /// <summary>
        /// For <see cref="SystemOneQuestionType.Choice"/>: the options (key) and what each one means (value). For
        /// <see cref="SystemOneQuestionType.Noul"/>: optionally the <c>true</c> / <c>false</c> conditions.
        /// </summary>
        public IDictionary<string, string>? Criteria { get; init; }

        /// <summary>Creates a yes/no question.</summary>
        /// <param name="instructions">The question.</param>
        /// <param name="whenTrue">Optionally, when the answer is <c>true</c>.</param>
        /// <param name="whenFalse">Optionally, when the answer is <c>false</c>.</param>
        public static SystemOneQuestion Noul(string instructions, string? whenTrue = null, string? whenFalse = null) {
            var criteria = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(whenTrue)) {
                criteria["true"] = whenTrue;
            }
            if (!string.IsNullOrWhiteSpace(whenFalse)) {
                criteria["false"] = whenFalse;
            }
            return new() { Type = SystemOneQuestionType.Noul, Instructions = instructions, Criteria = criteria.Count > 0 ? criteria : null };
        }

        /// <summary>Creates a single-choice question.</summary>
        /// <param name="instructions">The question.</param>
        /// <param name="options">The options and what each one means.</param>
        public static SystemOneQuestion Choice(string instructions, IDictionary<string, string> options) =>
            new() { Type = SystemOneQuestionType.Choice, Instructions = instructions, Criteria = options };
    }

    /// <summary>The answers of a system-one decision, returned by <see cref="Services.ISystemOneClient.DecideAsync"/>.</summary>
    public record SystemOneDecision
    {
        /// <summary>The answers, keyed by question name. A question the model did not answer is missing.</summary>
        public IReadOnlyDictionary<string, SystemOneAnswer> Answers { get; init; } = new Dictionary<string, SystemOneAnswer>();

        /// <summary>Token usage of the decision call.</summary>
        public UsageDetails? Usage { get; init; }

        /// <summary>Returns the answer to the question <paramref name="name"/>, or <c>null</c> when it was not asked or not answered.</summary>
        /// <param name="name">The question name.</param>
        public SystemOneAnswer? Get(string name) => Answers.TryGetValue(name, out var answer) ? answer : null;
    }

    /// <summary>The system-one model's answer to one <see cref="SystemOneQuestion"/>.</summary>
    public record SystemOneAnswer
    {
        /// <summary>The question kind this answers.</summary>
        public SystemOneQuestionType Type { get; init; }

        /// <summary>
        /// For a choice: the chosen option, spelled as in the question's criteria, or <c>null</c> when the model picked
        /// something that is not one of them. Otherwise <c>null</c>.
        /// </summary>
        public string? Choice { get; init; }

        /// <summary>For a noul: the probability (0..1) that the condition holds; otherwise <c>null</c>.</summary>
        public double? Probability { get; init; }

        /// <summary>For a noul: whether the condition holds (<see cref="Probability"/> of at least 0.5).</summary>
        public bool IsTrue => Probability >= 0.5;

        /// <summary>For a choice: the probability of each option; otherwise empty.</summary>
        public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();

        /// <summary>
        /// How sure the model is: the chosen option's probability for a choice (0 when <see cref="Choice"/> is <c>null</c>),
        /// <c>max(p, 1 - p)</c> for a noul. The caller compares it with its own threshold.
        /// </summary>
        public double Confidence { get; init; }
    }

    /// <summary>The body of a system-one decision response, as the endpoint returns it; mapped to a <see cref="SystemOneDecision"/>.</summary>
    internal sealed record SystemOneDecisionResponse
    {
        /// <summary>The deployed model that answered.</summary>
        public string? Model { get; init; }

        /// <summary>The answers, keyed by question name.</summary>
        public Dictionary<string, SystemOneAnswerPayload>? Answers { get; init; }

        /// <summary>The token usage.</summary>
        public SystemOneUsagePayload? Usage { get; init; }

        /// <summary>Maps the response to a <see cref="SystemOneDecision"/>, keeping only answers to the asked <paramref name="questions"/>.</summary>
        /// <param name="questions">The questions of the request, keyed by name.</param>
        public SystemOneDecision ToDecision(IReadOnlyDictionary<string, SystemOneQuestion> questions) {
            var answers = new Dictionary<string, SystemOneAnswer>();
            foreach (var (name, question) in questions) {
                if (Answers?.GetValueOrDefault(name)?.ToAnswer(question) is { } answer) {
                    answers[name] = answer;
                }
            }
            return new SystemOneDecision {
                Answers = answers,
                Usage = Usage is null ? null : new UsageDetails {
                    InputTokenCount = Usage.InputTokens,
                    OutputTokenCount = Usage.OutputTokens,
                    TotalTokenCount = (Usage.InputTokens ?? 0) + (Usage.OutputTokens ?? 0),
                },
            };
        }
    }

    /// <summary>One answer of a <see cref="SystemOneDecisionResponse"/>.</summary>
    internal sealed record SystemOneAnswerPayload
    {
        /// <summary>The question kind, <c>noul</c> or <c>choice</c>.</summary>
        public string? Type { get; init; }

        /// <summary>For a noul: the probability (0..1) that the condition holds.</summary>
        public double? Noul { get; init; }

        /// <summary>For a choice: the chosen option.</summary>
        public string? Choice { get; init; }

        /// <summary>For a choice: the probability of each option.</summary>
        public Dictionary<string, double>? Probabilities { get; init; }

        /// <summary>For a choice: the probability of the chosen option.</summary>
        public double? Confidence { get; init; }

        /// <summary>Maps the payload to a <see cref="SystemOneAnswer"/> for <paramref name="question"/>, or <c>null</c> when a noul has no probability.</summary>
        /// <param name="question">The question this answers.</param>
        public SystemOneAnswer? ToAnswer(SystemOneQuestion question) {
            if (question.Type == SystemOneQuestionType.Noul) {
                return Noul is not { } probability ? null : new SystemOneAnswer {
                    Type = SystemOneQuestionType.Noul,
                    Probability = probability,
                    Confidence = Math.Max(probability, 1 - probability),
                };
            }
            var probabilities = new Dictionary<string, double>(Probabilities ?? [], StringComparer.OrdinalIgnoreCase);
            // Map the answer back to the option exactly as the caller spelled it; anything else is not an answer.
            var chosenOption = question.Criteria?.Keys.FirstOrDefault(key => string.Equals(key, Choice, StringComparison.OrdinalIgnoreCase));
            var confidence = chosenOption is null ? 0
                : probabilities.TryGetValue(chosenOption, out var chosen) ? chosen
                : Confidence ?? 0;
            return new SystemOneAnswer {
                Type = SystemOneQuestionType.Choice,
                Choice = chosenOption,
                Probabilities = probabilities,
                Confidence = confidence,
            };
        }
    }

    /// <summary>The token usage of a <see cref="SystemOneDecisionResponse"/>.</summary>
    internal sealed record SystemOneUsagePayload
    {
        /// <summary>The input tokens.</summary>
        [JsonPropertyName("input_tokens")]
        public long? InputTokens { get; init; }

        /// <summary>The output tokens.</summary>
        [JsonPropertyName("output_tokens")]
        public long? OutputTokens { get; init; }
    }
}

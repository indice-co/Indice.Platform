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
        return JsonSerializer.Deserialize<SystemOneDecisionResponse>(payload, JsonOptions)?.ToDecision(request.Questions) ?? new SystemOneDecision();
    }
}

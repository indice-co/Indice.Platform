using System.ClientModel;
using System.ClientModel.Primitives;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>
/// <see cref="IAgentsModelProvider"/> for any OpenAI-compatible endpoint. It serves the roles that have a model configured in <see cref="FreeLLMOptions.Models"/>.
/// </summary>
/// <remarks>
/// Follows the rules of <see cref="IAgentsModelProvider"/>: it reads only <see cref="FreeLLMOptions"/> and resolves <see cref="IHttpClientFactory"/> lazily.
/// </remarks>
internal sealed class FreeLLMModelProvider : IAgentsModelProvider
{
    // The OpenAI client insists on a credential. FreeLLM's own documentation uses the same placeholder for endpoints that need no key.
    private const string PlaceholderApiKey = "unused";

    private readonly IOptions<FreeLLMOptions> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<OpenAIClient> _chatApi;
    private readonly Lazy<OpenAIClient> _embeddingApi;

    public FreeLLMModelProvider(IOptions<FreeLLMOptions> options, IServiceProvider serviceProvider) {
        _options = options;
        _serviceProvider = serviceProvider;
        _chatApi = new Lazy<OpenAIClient>(() => {
            var settings = _options.Value;
            return CreateClient(settings.Endpoint!, settings.ApiKey, settings, adaptChatCompletions: true);
        });
        _embeddingApi = new Lazy<OpenAIClient>(() => {
            var settings = _options.Value;
            if (string.IsNullOrWhiteSpace(settings.EmbeddingEndpoint)) {
                return _chatApi.Value;
            }
            // The chat key belongs to the chat host: a separate embeddings host gets only its own key.
            return CreateClient(settings.EmbeddingEndpoint, settings.EmbeddingApiKey, settings, adaptChatCompletions: false);
        });
    }

    public string Name => "FreeLLM";

    public bool Serves(AgentsModelRole role) {
        var models = _options.Value.Models;
        return !string.IsNullOrWhiteSpace(role switch {
            AgentsModelRole.Reasoning => models.Reasoning,
            AgentsModelRole.Fast => models.Fast,
            _ => models.Embedding
        });
    }

    public IChatClient CreateChatClient(AgentsModelRole role) {
        var settings = _options.Value;
        var model = role switch {
            AgentsModelRole.Reasoning => settings.Models.Reasoning,
            AgentsModelRole.Fast => settings.Models.Fast,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Chat clients exist only for the Reasoning and Fast roles.")
        };
        if (string.IsNullOrWhiteSpace(model)) {
            throw new InvalidOperationException($"{FreeLLMOptions.SectionName}:Models:{role} is not configured.");
        }
        IChatClient client = _chatApi.Value.GetChatClient(model).AsIChatClient();
        if (settings.Compatibility.StructuredOutput == FreeLLMStructuredOutput.JsonObject) {
            client = new JsonObjectResponseFormatChatClient(client);
        }
        return client;
    }

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() {
        var settings = _options.Value;
        var model = settings.Models.Embedding;
        if (string.IsNullOrWhiteSpace(model)) {
            throw new InvalidOperationException($"{FreeLLMOptions.SectionName}:Models:Embedding is not configured.");
        }
        var generator = _embeddingApi.Value.GetEmbeddingClient(model).AsIEmbeddingGenerator(settings.EmbeddingDimensions);
        return new DimensionCheckingEmbeddingGenerator(generator, model, settings.Compatibility.TruncateEmbeddings);
    }

    private OpenAIClient CreateClient(string endpoint, string? apiKey, FreeLLMOptions settings, bool adaptChatCompletions) {
        var httpClient = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(FreeLLMOptions.HttpClientName);
        var clientOptions = new OpenAIClientOptions {
            Endpoint = new Uri(endpoint.TrimEnd('/')),
            Transport = new HttpClientPipelineTransport(httpClient),
        };
        if (settings.NetworkTimeout is { } timeout) {
            clientOptions.NetworkTimeout = timeout;
        }
        if (adaptChatCompletions && ChatCompletionsCompatibilityPolicy.IsNeeded(settings.Compatibility)) {
            clientOptions.AddPolicy(new ChatCompletionsCompatibilityPolicy(settings.Compatibility.UseMaxTokens, settings.Compatibility.StreamUsage), PipelinePosition.PerCall);
        }
        return new OpenAIClient(new ApiKeyCredential(string.IsNullOrWhiteSpace(apiKey) ? PlaceholderApiKey : apiKey), clientOptions);
    }
}

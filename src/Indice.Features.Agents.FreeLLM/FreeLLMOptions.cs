using Indice.Features.Agents.Core;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>
/// Settings of the FreeLLM model provider: any OpenAI-compatible <c>/v1</c> endpoint, such as a FreeLLM gateway, Ollama or OpenRouter.
/// Bound from the <c>Dex:FreeLLM</c> configuration section.
/// </summary>
/// <remarks>
/// The provider serves exactly the roles that have a model configured in <see cref="Models"/>. With no model configured it is inactive,
/// so <c>UseFreeLLM()</c> can be called unconditionally and the environment's configuration decides.
/// </remarks>
public class FreeLLMOptions
{
    /// <summary>The configuration section the options are bound from.</summary>
    public const string SectionName = "Dex:FreeLLM";

    /// <summary>The name of the <see cref="HttpClient"/> the provider sends its requests with. Hosts can add handlers (proxies, resilience) to it.</summary>
    public const string HttpClientName = "Indice.Agents.FreeLLM";

    /// <summary>The base address of the OpenAI-compatible API including its version segment, e.g. <c>http://localhost:3000/v1</c> or <c>http://localhost:11434/v1</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The API key sent as a bearer token. Optional: a placeholder is sent when the endpoint needs none.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The model names per role.</summary>
    public FreeLLMModels Models { get; set; } = new();

    /// <summary>
    /// The base address of the OpenAI-compatible API that serves embeddings, when it differs from <see cref="Endpoint"/> (some gateways have no
    /// <c>/v1/embeddings</c> endpoint). Defaults to <see cref="Endpoint"/>.
    /// </summary>
    public string? EmbeddingEndpoint { get; set; }

    /// <summary>
    /// The API key for <see cref="EmbeddingEndpoint"/>. When <see cref="EmbeddingEndpoint"/> is not set the embeddings use <see cref="ApiKey"/>;
    /// the chat key is never sent to a different embeddings host.
    /// </summary>
    public string? EmbeddingApiKey { get; set; }

    /// <summary>
    /// When set, the requested vector width is sent as <c>dimensions</c> to models that can shorten their output. It must be
    /// <see cref="AgentsOptions.EmbeddingDimensionsDefault"/>, the width of the knowledge store's vector column. Leave empty for models
    /// that produce that width natively.
    /// </summary>
    public int? EmbeddingDimensions { get; set; }

    /// <summary>The timeout of a single request to the model endpoint. Defaults to the OpenAI client's default (100 seconds).</summary>
    public TimeSpan? NetworkTimeout { get; set; }

    /// <summary>Switches that adapt the requests to servers that are not strictly OpenAI.</summary>
    public FreeLLMCompatibilityOptions Compatibility { get; set; } = new();
}

/// <summary>The model names the FreeLLM provider serves, per role. A role without a model is not served by the provider.</summary>
public class FreeLLMModels
{
    /// <summary>The model for the reasoning role: intent classification, routing, tool calling and answer composition.</summary>
    public string? Reasoning { get; set; }

    /// <summary>The model for the fast role: query rewriting and reranking.</summary>
    public string? Fast { get; set; }

    /// <summary>The embedding model. It must produce <see cref="AgentsOptions.EmbeddingDimensionsDefault"/>-dimensional vectors.</summary>
    public string? Embedding { get; set; }
}

/// <summary>Switches that adapt the requests to servers that are not strictly OpenAI.</summary>
public class FreeLLMCompatibilityOptions
{
    /// <summary>How structured (typed) replies are requested. Defaults to <see cref="FreeLLMStructuredOutput.JsonSchema"/>.</summary>
    public FreeLLMStructuredOutput StructuredOutput { get; set; } = FreeLLMStructuredOutput.JsonSchema;

    /// <summary>
    /// Sends the output limit as <c>max_tokens</c> instead of <c>max_completion_tokens</c>. Most OpenAI-compatible servers only understand the former, and ignore
    /// the latter. Set it to <c>false</c> for OpenAI reasoning models, which reject <c>max_tokens</c>. Defaults to <c>true</c>.
    /// </summary>
    public bool UseMaxTokens { get; set; } = true;

    /// <summary>
    /// Asks streamed responses to report token usage (<c>stream_options.include_usage</c>), which the per-session token limit relies on. Set it to <c>false</c>
    /// for strict servers that reject the option; the token usage of streamed answers, and with it the limit, is then not tracked. Defaults to <c>true</c>.
    /// </summary>
    public bool StreamUsage { get; set; } = true;

    /// <summary>
    /// Shortens embedding vectors that are longer than <see cref="AgentsOptions.EmbeddingDimensionsDefault"/> to that width and rescales them to unit length,
    /// instead of rejecting them. Use it only for models trained for shortened ("Matryoshka") vectors, such as <c>gemini-embedding-001</c>, OpenAI's
    /// <c>text-embedding-3</c> models and Qwen3-Embedding; for other models truncation silently degrades search quality. Shorter vectors are always rejected.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool TruncateEmbeddings { get; set; }
}

/// <summary>How structured (typed) replies are requested from the model.</summary>
public enum FreeLLMStructuredOutput
{
    /// <summary>Uses <c>response_format: json_schema</c>, which the server enforces. Supported by OpenAI, and by many gateways and local servers.</summary>
    JsonSchema = 0,

    /// <summary>
    /// Uses <c>response_format: json_object</c> and puts the schema in the instructions instead. For servers that do not support <c>json_schema</c>;
    /// the model follows the schema on a best-effort basis.
    /// </summary>
    JsonObject = 1
}

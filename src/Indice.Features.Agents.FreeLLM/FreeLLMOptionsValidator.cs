using Indice.Features.Agents.Core;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>Validates <see cref="FreeLLMOptions"/>. With no model configured the provider is inactive and nothing is required.</summary>
internal sealed class FreeLLMOptionsValidator : IValidateOptions<FreeLLMOptions>
{
    public ValidateOptionsResult Validate(string? name, FreeLLMOptions options) {
        var failures = new List<string>();
        var models = options.Models;
        var hasChatModel = !string.IsNullOrWhiteSpace(models.Reasoning) || !string.IsNullOrWhiteSpace(models.Fast);
        var hasEmbeddingModel = !string.IsNullOrWhiteSpace(models.Embedding);

        if (hasChatModel && !IsHttpUri(options.Endpoint)) {
            failures.Add($"{FreeLLMOptions.SectionName}:Endpoint must be an absolute http(s) URL (e.g. http://localhost:3000/v1) when a chat model is configured.");
        }
        if (hasEmbeddingModel && !IsHttpUri(string.IsNullOrWhiteSpace(options.EmbeddingEndpoint) ? options.Endpoint : options.EmbeddingEndpoint)) {
            failures.Add($"{FreeLLMOptions.SectionName}:EmbeddingEndpoint (or {FreeLLMOptions.SectionName}:Endpoint) must be an absolute http(s) URL when an embedding model is configured.");
        }
        if (options.EmbeddingDimensions is { } dimensions && dimensions != AgentsOptions.EmbeddingDimensionsDefault) {
            failures.Add($"{FreeLLMOptions.SectionName}:EmbeddingDimensions must be {AgentsOptions.EmbeddingDimensionsDefault}, the width of the knowledge store's vector column, or left empty.");
        }
        if (options.NetworkTimeout is { } timeout && timeout <= TimeSpan.Zero) {
            failures.Add($"{FreeLLMOptions.SectionName}:NetworkTimeout must be greater than zero.");
        }
        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    private static bool IsHttpUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

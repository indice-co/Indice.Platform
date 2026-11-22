using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.FreeLLM;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions to plug the FreeLLM model provider into the agents feature.</summary>
public static class FreeLLMModelProviderBuilderExtensions
{
    /// <summary>
    /// Serves the model roles that have a model configured in <c>Dex:FreeLLM:Models</c> from an OpenAI-compatible endpoint (a FreeLLM gateway, Ollama, OpenRouter and similar).
    /// Roles without a model keep being served by Azure AI Foundry.
    /// </summary>
    /// <remarks>The provider is inactive until a model is configured, so this can be called unconditionally.</remarks>
    /// <param name="builder">The model provider builder.</param>
    /// <param name="configure">An optional delegate that overrides the settings bound from the <c>Dex:FreeLLM</c> configuration section.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddAgents(configuration, options => options.ConfigureModelProviders = providers => providers.UseFreeLLM());
    /// </code>
    /// </example>
    public static AgentsModelProviderBuilder UseFreeLLM(this AgentsModelProviderBuilder builder, Action<FreeLLMOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(builder);
        var options = builder.Services.AddOptions<FreeLLMOptions>().BindConfiguration(FreeLLMOptions.SectionName);
        if (configure is not null) {
            options.Configure(configure);
        }
        options.ValidateOnStart();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<FreeLLMOptions>, FreeLLMOptionsValidator>());
        // LLM calls outlive a typical HTTP timeout; FreeLLMOptions.NetworkTimeout governs instead.
#pragma warning disable EXTEXP0001
        builder.Services.AddHttpClient(FreeLLMOptions.HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        return builder.AddModelProvider<FreeLLMModelProvider>();
    }
}
